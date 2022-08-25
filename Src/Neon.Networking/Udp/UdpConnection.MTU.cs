using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Neon.Networking.Udp.Messages;
using Neon.Util;

namespace Neon.Networking.Udp
{
	public partial class UdpConnection
	{
        public const int INITIAL_MTU = 508;

        enum MtuExpansionStatus
        {
            NotStarted = 0,
            Started = 1,
            Finished = 2
        }

        bool mtuExpand;
        int mtuExpandMaxFailAttempts;
        int mtuExpandFrequency;

        int mtuFailedAttempts = -1;
        int smallestFailedMtu = -1;
        MtuExpansionStatus mtuStatus;
        DateTime lastMtuExpandSent;

        public void ExpandMTU()
        {
            if (mtuExpand && mtuStatus == MtuExpansionStatus.NotStarted)
                mtuStatus = MtuExpansionStatus.Started;
        }


        void OnMtuSuccess(Datagram datagram)
        {
            try
            {
                if (!CheckStatusForDatagram(datagram, UdpConnectionStatus.Connected))
                    return;


                int size = datagram.ReadVarInt32();
                bool fix = datagram.ReadByte() == 1;
                if (size > this.Mtu)
                {
                    logger.Debug($"#{Id} MTU Successfully expanded to {size}");
                    this.Mtu = size;
                    if (!fix)
                        SendNextMtuExpand();
                }

                if (fix)
                {
                    logger.Debug($"#{Id} the other side asks us to fix MTU on {size}");
                    FixMtu();
                }
            }
            finally
            {
                datagram.Dispose();
            }
        }

        void FixMtu()
        {
            if (mtuStatus != MtuExpansionStatus.Started)
                return;

            logger.Debug($"#{Id} fixing MTU {this.Mtu}");
            mtuStatus = MtuExpansionStatus.Finished;
        }

        void OnMtuExpand(Datagram datagram)
        {
            try
            {
                if (!CheckStatusForDatagram(datagram, UdpConnectionStatus.Connected))
                    return;

                int size = datagram.GetTotalSize();
                byte fix = 0;
                if (size > peer.Configuration.LimitMtu)
                {
                    size = peer.Configuration.LimitMtu;
                    fix = 1;
                }

                logger.Debug($"#{Id} MTU Successfully expanded to {size} by request from other side");
                var mtuDatagram = peer.CreateDatagram(MessageType.ExpandMTUSuccess, serviceUnreliableChannel.Descriptor, 5);
                mtuDatagram.WriteVarInt(size);
                mtuDatagram.Write(fix);

                if (size > this.Mtu)
                    this.Mtu = size;
                serviceUnreliableChannel.SendDatagramAsync(mtuDatagram);
            }
            finally
            {
                datagram.Dispose();
            }
        }

        void SendNextMtuExpand()
        {
            int nextMtu = 0;

            if (smallestFailedMtu < 0)
                nextMtu = Math.Min(ushort.MaxValue, (int)(this.Mtu * 1.25));
            else
                nextMtu = (int)(((float)smallestFailedMtu + (float)Mtu) / 2.0f);

            if (nextMtu > peer.Configuration.LimitMtu)
                nextMtu = peer.Configuration.LimitMtu;

            if (nextMtu == Mtu)
            {
                FixMtu();
                return;
            }

            lastMtuExpandSent = DateTime.UtcNow;
            int size = nextMtu - Datagram.GetHeaderSize(false);
            var mtuDatagram = peer.CreateDatagram(MessageType.ExpandMTURequest, serviceUnreliableChannel.Descriptor, size);
            mtuDatagram.Length = size;
            if (mtuDatagram.GetTotalSize() != nextMtu)
                throw new Exception("Datagram total size doesn't match header+body size. Perhaps header size calculation failed");

            logger.Debug($"#{Id} expanding MTU to {nextMtu}...");
            serviceUnreliableChannel.SendDatagramAsync(mtuDatagram).ContinueWith(t =>
            {
                Exception ex = t.Exception.GetInnermostException();
                SocketException sex = ex as SocketException;
                logger.Debug($"#{Id} MTU {nextMtu} expand failed ({ex.Message})");
                if (sex != null && sex.SocketErrorCode == SocketError.MessageSize)
                {
                    if (smallestFailedMtu < 1 || nextMtu < smallestFailedMtu)
                    {
                        smallestFailedMtu = nextMtu;
                        mtuFailedAttempts++;
                        if (mtuFailedAttempts >= mtuExpandMaxFailAttempts)
                        {
                            FixMtu();
                            return;
                        }

                        SendNextMtuExpand();
                    }
                }
                else
                {
                    FixMtu();
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        void MtuCheck()
        {
            if (mtuStatus != MtuExpansionStatus.Started)
                return;

            if ((DateTime.UtcNow - lastMtuExpandSent).TotalMilliseconds > mtuExpandFrequency)
            {
                mtuFailedAttempts++;
                if (mtuFailedAttempts >= mtuExpandMaxFailAttempts)
                {
                    FixMtu();
                    return;
                }

                SendNextMtuExpand();
                return;
            }
        }
    }
}
