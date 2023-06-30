using System;
using System.IO;
using System.Text;
using Neon.Networking.IO;
using Neon.Networking.Messages;
using Neon.Util.Pooling;

namespace Neon.Networking.Udp.Messages
{
    class Datagram : BaseRawMessage
    {
        static readonly ArraySegment<byte> EMPTY_SEGMENT = new ArraySegment<byte>(new byte[0], 0, 0);

        public MessageType Type { get; set; }
        public bool Compressed { get; set; }
        public bool Encrypted { get; set; }
        public ushort Sequence { get; set; }
        public bool IsFragmented { get; private set; }
        public FragmentInfo FragmentationInfo { get; private set; }
        public DeliveryType DeliveryType { get; set; }

        public int Channel
        {
            get => _channel;
            set
            {
                ChannelDescriptor.CheckChannelValue(value);
                _channel = value;
            }
        }

        int _channel;

        internal Datagram(IMemoryManager memoryManager)
            : base(memoryManager)
        {
        }

        internal Datagram(IMemoryManager memoryManager, int length)
            : base(memoryManager, length)
        {
        }

        internal Datagram(IMemoryManager memoryManager, ArraySegment<byte> arraySegment)
            : this(memoryManager, arraySegment, DEFAULT_ENCODING, Guid.NewGuid())
        {
        }

        internal Datagram(IMemoryManager memoryManager, ArraySegment<byte> arraySegment, Guid guid)
            : this(memoryManager, arraySegment, DEFAULT_ENCODING, guid)
        {
        }

        internal Datagram(IMemoryManager memoryManager, ArraySegment<byte> arraySegment, Encoding encoding, Guid guid)
            : base(memoryManager, arraySegment, encoding, guid)
        {
        }

        public static Datagram Parse(IMemoryManager memoryManager, ArraySegment<byte> data)
        {
            return Parse(memoryManager, data, DEFAULT_ENCODING);
        }

        public static Datagram Parse(IMemoryManager memoryManager, ArraySegment<byte> data, Encoding encoding)
        {
            var reader = new ByteArrayReader(data);
            byte serviceByte = reader.ReadByte();
            byte serviceByte2 = reader.ReadByte();
            int deliveryMethod = (serviceByte & 0b_1110_0000) >> 5;
            int channel = (byte) ((serviceByte & 0b_0001_1100) >> 2);
            bool fragmented = (serviceByte & 0b_0000_0010) >> 1 == 1;
            bool compressed = (serviceByte & 0b_0000_0001) == 1;

            var datagramType = (MessageType) ((serviceByte2 & 0b_1111_0000) >> 4);
            bool encrypted = (serviceByte2 & 0b_0000_1000) >> 3 == 1;
            //free bit serviceByte2 0b_0000_1111

            var deliveryType = (DeliveryType) deliveryMethod;
            ushort sequence = reader.ReadUInt16();
            FragmentInfo fragmentInfo = default;
            if (fragmented)
                fragmentInfo = new FragmentInfo(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());

            var guid = Guid.NewGuid();
            // RecyclableMemoryStream stream = null;
            ArraySegment<byte> segment;
            if (!reader.EOF)
                segment = reader.ReadArraySegment(reader.Count - reader.Position);
            else
                segment = EMPTY_SEGMENT;

            var datagram = new Datagram(memoryManager, segment, guid);
            datagram.Guid = guid;
            datagram._channel = channel;
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

            var writer = new ByteArrayWriter(segment);
            var serviceByte = 0;

            serviceByte |= (byte) DeliveryType << 5;
            serviceByte |= Channel << 2;
            if (Compressed)
                serviceByte |= 0b_0000_0001;
            if (IsFragmented)
                serviceByte |= 0b_0000_0010;
            writer.Write((byte) serviceByte);

            serviceByte = 0;
            serviceByte |= (byte) Type << 4;
            if (Encrypted)
                serviceByte |= 0b_0000_1000;
            writer.Write((byte) serviceByte);

            writer.Write(Sequence);

            if (IsFragmented)
            {
                writer.Write(FragmentationInfo.FragmentationGroupId);
                writer.Write(FragmentationInfo.Frame);
                writer.Write(FragmentationInfo.Frames);
            }

            if (_stream != null)
            {
                if (writer.GetBytesLeft() < _stream.Length)
                    throw new IOException($"The provided {nameof(ArraySegment<byte>)} has not enough space");

                _stream.Position = 0;
                _stream.Read(segment.Array, segment.Offset + writer.Position, (int) _stream.Length);
                writer.Position += (int) _stream.Length;
            }

            return writer.Position;
        }

        public void SetFragmentation(FragmentInfo fragmentInfo)
        {
            CheckDisposed();
            IsFragmented = true;
            FragmentationInfo = fragmentInfo;
        }

        public void SetNoFragmentation()
        {
            CheckDisposed();
            IsFragmented = false;
        }

        public int GetTotalSize()
        {
            CheckDisposed();
            int size = GetHeaderSize();
            if (_stream != null)
                size += (int) _stream.Length;
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
            return GetHeaderSize(IsFragmented);
        }

        public ChannelDescriptor GetChannelDescriptor()
        {
            return new ChannelDescriptor(Channel, DeliveryType);
        }

        public Datagram CreateAck()
        {
            var ack = new Datagram(_memoryManager, EMPTY_SEGMENT);
            ack.Type = MessageType.DeliveryAck;
            ack.Sequence = Sequence;
            ack._channel = _channel;
            ack.DeliveryType = DeliveryType;
            return ack;
        }

        public void UpdateForChannelDescriptor(ChannelDescriptor channelDescriptor)
        {
            DeliveryType = channelDescriptor.DeliveryType;
            Channel = channelDescriptor.Channel;
        }

        public override string ToString()
        {
            var fragInfo = "none";
            if (IsFragmented)
                fragInfo =
                    $"{FragmentationInfo.Frame}/{FragmentationInfo.Frames}(g{FragmentationInfo.FragmentationGroupId})";
            var len = GetHeaderSize().ToString();
            if (_stream != null && !_disposed)
                len += "+" + _stream.Length;
            return
                $"{nameof(Datagram)}[g={Guid},type={Type},dtype={DeliveryType},channel={Channel},seq={Sequence},len={len},frag={fragInfo},comp={Compressed},enc={Encrypted}]";
        }

        public struct FragmentInfo
        {
            public ushort Frame { get; }
            public ushort Frames { get; }
            public ushort FragmentationGroupId { get; }

            public FragmentInfo(ushort groupId, ushort frame, ushort frames)
            {
                FragmentationGroupId = groupId;
                Frame = frame;
                Frames = frames;
            }
        }
    }
}