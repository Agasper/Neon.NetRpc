using System;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp.Channels
{
    struct PendingPacket
    {
        public Datagram Datagram { get; private set; }
        public int ReSendNum { get; private set; }
        public DateTime Timestamp { get; private set; }
        public bool AckReceived { get; private set; }

        public void Init(Datagram datagram)
        {
            Timestamp = DateTime.UtcNow;
            Datagram = datagram;
            ReSendNum = 0;
            AckReceived = false;
        }

        public int GetDelay()
        {
            return (int) (DateTime.UtcNow - Timestamp).TotalMilliseconds;
        }

        public bool GotAck()
        {
            if (Datagram == null)
                return false;
            AckReceived = true;
            return true;
        }

        public void Clear()
        {
            Datagram?.Dispose();
            Datagram = null;
            AckReceived = false;
        }

        public bool TryReSend(int resendDelay, bool multiplyOnSendNum)
        {
            Datagram datagram = Datagram;
            if (datagram == null)
                return false;
            if (AckReceived)
                return false;

            int actualDelay = resendDelay;
            if (multiplyOnSendNum)
                actualDelay *= ReSendNum + 1;
            if ((DateTime.UtcNow - Timestamp).TotalMilliseconds < actualDelay)
                return false;

            ReSendNum++;
            Timestamp = DateTime.UtcNow;
            return true;
        }
    }
}