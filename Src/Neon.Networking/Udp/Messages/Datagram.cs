using System;
using System.IO;
using System.Text;
using Microsoft.IO;
using Neon.Networking.IO;
using Neon.Networking.Messages;
using Neon.Util.Pooling;

namespace Neon.Networking.Udp.Messages
{
    class Datagram : BaseRawMessage
    {
        public struct FragmentInfo
        {
            public ushort Frame { get; }
            public ushort Frames { get;  }
            public ushort FragmentationGroupId { get; }

            public FragmentInfo(ushort groupId, ushort frame, ushort frames)
            {
                this.FragmentationGroupId = groupId;
                this.Frame = frame;
                this.Frames = frames;
            }
        }
        
        public MessageType Type { get; set; }
        public bool Compressed { get; set; }
        public bool Encrypted { get; set; }
        public ushort Sequence { get; set; }
        public bool IsFragmented { get; private set; }
        public FragmentInfo FragmentationInfo { get; private set; }
        public DeliveryType DeliveryType { get; set; }
        public int Channel
        {
            get => channel;
            set
            {
                ChannelDescriptor.CheckChannelValue(value);
                channel = value;
            }
        }
        
        int channel;
        
        public static Datagram Parse(IMemoryManager memoryManager, ArraySegment<byte> data)
        {
            return Parse(memoryManager, data, DEFAULT_ENCODING);
        }

        public static Datagram Parse(IMemoryManager memoryManager, ArraySegment<byte> data, Encoding encoding)
        {
            ByteArrayReader reader = new ByteArrayReader(data);
            byte serviceByte = reader.ReadByte();
            byte serviceByte2 = reader.ReadByte();
            int deliveryMethod = (serviceByte & 0b_1110_0000) >> 5;
            int channel = (byte)((serviceByte & 0b_0001_1100) >> 2);
            bool fragmented = (serviceByte & 0b_0000_0010) >> 1 == 1;
            bool compressed = (serviceByte & 0b_0000_0001) == 1;

            MessageType datagramType = (MessageType)((serviceByte2 & 0b_1111_0000) >> 4);
            bool encrypted = ((serviceByte2 & 0b_0000_1000) >> 3) == 1;
            //free bit serviceByte2 0b_0000_1111

            DeliveryType deliveryType = (DeliveryType)deliveryMethod;
            ushort sequence = reader.ReadUInt16();
            FragmentInfo fragmentInfo = default;
            if (fragmented)
                fragmentInfo = new FragmentInfo(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());

            System.Guid guid = Guid.NewGuid();
            RecyclableMemoryStream stream = null;
            if (!reader.EOF)
            {
                var payloadSegment = reader.ReadArraySegment(reader.Count - reader.Position);
                stream = memoryManager.GetStream(payloadSegment.Count, guid);
                stream.Write(payloadSegment.Array, payloadSegment.Offset, payloadSegment.Count);
                stream.Position = 0;
            }

            var datagram = new Datagram(memoryManager, stream, guid);
            datagram.Guid = guid;
            datagram.channel = channel;
            datagram.Compressed = compressed;
            datagram.Encrypted = encrypted;
            datagram.Sequence = sequence;
            datagram.Type = datagramType;
            datagram.DeliveryType = deliveryType;
            datagram.IsFragmented = fragmented;
            if (fragmented)
                datagram.FragmentationInfo = fragmentInfo;
            return datagram;
        }
        
        public int BuildTo(ArraySegment<byte> segment)
        {
            ChannelDescriptor.CheckChannelValue(Channel);

            ByteArrayWriter writer = new ByteArrayWriter(segment);
            int serviceByte = 0;

            serviceByte |= (byte)DeliveryType << 5;
            serviceByte |= Channel << 2;
            if (Compressed)
                serviceByte |= 0b_0000_0001;
            if (IsFragmented)
                serviceByte |= 0b_0000_0010;
            writer.Write((byte)serviceByte);

            serviceByte = 0;
            serviceByte |= (byte)Type << 4;
            if (Encrypted)
                serviceByte |= 0b_0000_1000;
            writer.Write((byte)serviceByte);
            
            writer.Write(Sequence);

            if (IsFragmented)
            {
                writer.Write(this.FragmentationInfo.FragmentationGroupId);
                writer.Write(this.FragmentationInfo.Frame);
                writer.Write(this.FragmentationInfo.Frames);
            }

            if (stream != null)
            {
                if (writer.GetBytesLeft() < stream.Length)
                    throw new IOException($"The provided {nameof(ArraySegment<byte>)} has not enough space");
                
                stream.Position = 0;
                stream.Read(segment.Array, segment.Offset + writer.Position, (int)stream.Length);
                writer.Position += (int)stream.Length;
            }

            return writer.Position;
        }

        
        internal Datagram(IMemoryManager memoryManager, RecyclableMemoryStream stream, Guid guid)
            : this(memoryManager, stream, DEFAULT_ENCODING, guid)
        {
        }

        internal Datagram(IMemoryManager memoryManager, RecyclableMemoryStream stream, Encoding encoding, Guid guid)
            : base(memoryManager, stream, encoding, guid)
        {
        }

        public void SetFragmentation(FragmentInfo fragmentInfo)
        {
            CheckDisposed();
            this.IsFragmented = true;
            this.FragmentationInfo = fragmentInfo;
        }
        
        public void SetNoFragmentation()
        {
            CheckDisposed();
            this.IsFragmented = false;
        }

        public int GetTotalSize()
        {
            CheckDisposed();
            int size = GetHeaderSize();
            if (stream != null)
                size += (int)stream.Length;
            return size;
        }

        public static int GetHeaderSize(bool fragmented)
        {
            int result = 1 + 1 + 2;
            if (fragmented)
                result += 2 + 2 + 2;
            return result;
        }

        public int GetHeaderSize()
        {
            return GetHeaderSize(this.IsFragmented);
        }

        public ChannelDescriptor GetChannelDescriptor()
        {
            return new ChannelDescriptor(Channel, DeliveryType);
        }

        public Datagram CreateAck()
        {
            Datagram ack = new Datagram(this.memoryManager, null, Guid.NewGuid());
            ack.Type = MessageType.DeliveryAck;
            ack.Sequence = this.Sequence;
            ack.channel = this.channel;
            ack.DeliveryType = this.DeliveryType;
            return ack;
        }

        public void UpdateForChannelDescriptor(ChannelDescriptor channelDescriptor)
        {
            this.DeliveryType = channelDescriptor.DeliveryType;
            this.Channel = channelDescriptor.Channel;
        }
       
        public override string ToString()
        {
            string fragInfo = "none";
            if (IsFragmented)
                fragInfo = $"{FragmentationInfo.Frame}/{FragmentationInfo.Frames}(g{FragmentationInfo.FragmentationGroupId})";
            string len = GetHeaderSize().ToString();
            if (stream != null && !disposed)
                len += "+" + stream.Length.ToString();
            return $"{nameof(Datagram)}[g={Guid},type={Type},dtype={DeliveryType},channel={Channel},seq={Sequence},len={len},frag={fragInfo}]";
        }

    }
}
