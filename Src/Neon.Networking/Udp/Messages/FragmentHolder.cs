using System;
using System.Collections.Generic;
using Neon.Networking.Messages;

namespace Neon.Networking.Udp.Messages
{
    class FragmentHolder : IDisposable
    {
        public DateTime Created { get; private set; }
        public bool IsCompleted => Frames == _receivedFrames;
        public IReadOnlyCollection<Datagram> Datagrams => _datagrams;
        public ushort Frames { get; }
        public ushort FragmentationGroupId { get; }
        readonly Datagram[] _datagrams;
        bool _disposed;

        int _receivedFrames;

        public FragmentHolder(ushort groupId, ushort frames)
        {
            FragmentationGroupId = groupId;
            Frames = frames;
            Created = DateTime.UtcNow;
            _datagrams = new Datagram[frames];
        }

        public void Dispose()
        {
            for (var i = 0; i < _datagrams.Length; i++)
            {
                Datagram d = _datagrams[i];
                if (d != null)
                    d.Dispose();
                _datagrams[i] = null;
            }

            _disposed = true;
        }

        void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FragmentHolder));
        }

        public UdpMessageInfo Merge(UdpPeer peer)
        {
            CheckDisposed();
            if (!IsCompleted)
                throw new InvalidOperationException($"{nameof(FragmentHolder)} isn't completed");

            if (_datagrams.Length == 0)
                throw new ArgumentException("Datagrams collection is empty");

            var payloadSize = 0;
            for (var i = 0; i < _datagrams.Length; i++)
            {
                Datagram datagram = _datagrams[i];
                payloadSize += datagram.Length;
            }

            Datagram head = _datagrams[0];

            RawMessage message = peer.CreateMessage(payloadSize, head.Compressed, head.Encrypted);

            for (var i = 0; i < _datagrams.Length; i++)
            {
                Datagram datagram = _datagrams[i];
                if (datagram.Length > 0)
                {
                    datagram.Position = 0;
                    datagram.CopyTo(message);
                }
            }

            return new UdpMessageInfo(message, head.DeliveryType, head.Channel);
        }

        public bool SetFrame(Datagram datagram)
        {
            CheckDisposed();
            if (datagram == null)
                throw new ArgumentNullException(nameof(datagram));
            if (!datagram.IsFragmented)
                throw new ArgumentException($"{nameof(FragmentHolder)} accepts only fragmented datagrams");
            if (datagram.FragmentationInfo.FragmentationGroupId != FragmentationGroupId)
                throw new ArgumentException($"{nameof(Datagram)} wrong fragmentation group id");
            if (datagram.FragmentationInfo.Frame >= _datagrams.Length)
                throw new ArgumentOutOfRangeException($"Frame out of range values 0-{_datagrams.Length - 1}");

            if (_datagrams[datagram.FragmentationInfo.Frame] != null)
                return false;

            _datagrams[datagram.FragmentationInfo.Frame] = datagram;
            _receivedFrames++;
            return true;
        }
    }
}