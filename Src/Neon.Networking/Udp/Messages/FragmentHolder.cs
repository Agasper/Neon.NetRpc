using System;
using System.Collections.Generic;
using System.Linq;
using Neon.Networking.Messages;
using Neon.Util.Pooling;

namespace Neon.Networking.Udp.Messages
{
    class FragmentHolder : IDisposable
    {
        public DateTime Created { get; private set; }
        public bool IsCompleted => Frames == receivedFrames;
        public IReadOnlyCollection<Datagram> Datagrams => datagrams;
        public ushort Frames { get; private set; }
        public ushort FragmentationGroupId { get; private set; }

        readonly Datagram[] datagrams;

        int receivedFrames;
        bool disposed;

        public FragmentHolder(ushort groupId, ushort frames)
        {
            FragmentationGroupId = groupId;
            Frames = frames;
            Created = DateTime.UtcNow;
            datagrams = new Datagram[frames];
        }

        public void Dispose()
        {
            for(int i = 0; i < datagrams.Length; i++)
            {
                var d = datagrams[i];
                if (d != null)
                    d.Dispose();
                datagrams[i] = null;
            }
            disposed = true;
        }

        void CheckDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(FragmentHolder));
        }

        public UdpRawMessage Merge(UdpPeer peer)
        {
            CheckDisposed();
            if (!IsCompleted)
                throw new InvalidOperationException($"{nameof(FragmentHolder)} isn't completed");

            if (datagrams.Length == 0)
                throw new ArgumentException("Datagrams collection is empty");

            int payloadSize = 0;
            for (int i = 0; i < datagrams.Length; i++)
            {
                Datagram datagram = datagrams[i];
                payloadSize += datagram.Length;
            }

            Datagram head = datagrams[0];

            RawMessage message = peer.CreateMessage(payloadSize, head.Compressed, head.Encrypted);

            for(int i =0; i < datagrams.Length; i++)
            {
                Datagram datagram = datagrams[i];
                if (datagram.Length > 0)
                {
                    datagram.Position = 0;
                    datagram.CopyTo(message);
                }
            }

            return new UdpRawMessage(message, head.DeliveryType, head.Channel);
        }

        public bool SetFrame(Datagram datagram)
        {
            CheckDisposed();
            if (datagram == null)
                throw new ArgumentNullException(nameof(datagram));
            if (!datagram.IsFragmented)
                throw new ArgumentException($"{nameof(FragmentHolder)} accepts only fragmented datagrams");
            if (datagram.FragmentationInfo.FragmentationGroupId != this.FragmentationGroupId)
                throw new ArgumentException($"{nameof(Datagram)} wrong fragmentation group id");
            if (datagram.FragmentationInfo.Frame >= datagrams.Length)
                throw new ArgumentOutOfRangeException($"Frame out of range values 0-{datagrams.Length-1}");

            if (datagrams[datagram.FragmentationInfo.Frame] != null)
                return false;

            datagrams[datagram.FragmentationInfo.Frame] = datagram;
            receivedFrames++;
            return true;
        }
    }
}
