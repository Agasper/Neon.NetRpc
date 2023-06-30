using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Neon.Networking.Udp.Messages;
using Neon.Util;

namespace Neon.Networking.Udp
{
    public partial class UdpConnection
    {
        public const int INITIAL_MTU = 508;

        readonly bool _mtuExpand;
        readonly int _mtuExpandFrequency;
        readonly int _mtuExpandMaxFailAttempts;
        DateTime _lastMtuExpandSent;

        int _mtuFailedAttempts = -1;
        MtuExpansionStatus _mtuStatus;
        int _smallestFailedMtu = -1;

        void ExpandMTU()
        {
            if (_mtuExpand && _mtuStatus == MtuExpansionStatus.NotStarted)
                _mtuStatus = MtuExpansionStatus.Started;
        }


        void OnMtuSuccess(Datagram datagram)
        {
            try
            {
                if (!CheckStatusForDatagram(datagram, UdpConnectionStatus.Connected))
                    return;


                int size = datagram.ReadVarInt32();
                bool fix = datagram.ReadByte() == 1;
                if (size > Mtu)
                {
                    _logger.Debug($"#{Id} MTU Successfully expanded to {size}");
                    Mtu = size;
                    if (!fix)
                        SendNextMtuExpand();
                }

                if (fix)
                {
                    _logger.Debug($"#{Id} the other side asks us to fix MTU on {size}");
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
            if (_mtuStatus != MtuExpansionStatus.Started)
                return;

            _logger.Debug($"#{Id} fixing MTU {Mtu}");
            _mtuStatus = MtuExpansionStatus.Finished;
        }

        void OnMtuExpand(Datagram datagram)
        {
            try
            {
                if (!CheckStatusForDatagram(datagram, UdpConnectionStatus.Connected))
                    return;

                int size = datagram.GetTotalSize();
                byte fix = 0;
                if (size > Parent.Configuration.LimitMtu)
                {
                    size = Parent.Configuration.LimitMtu;
                    fix = 1;
                }

                _logger.Debug($"#{Id} MTU Successfully expanded to {size} by request from other side");
                Datagram mtuDatagram = Parent.CreateDatagram(MessageType.ExpandMTUSuccess,
                    _serviceUnreliableChannel.Descriptor, 5);
                mtuDatagram.WriteVarInt(size);
                mtuDatagram.Write(fix);

                if (size > Mtu)
                    Mtu = size;
                _serviceUnreliableChannel.SendDatagramAsync(mtuDatagram, this.CancellationToken);
            }
            finally
            {
                datagram.Dispose();
            }
        }

        void SendNextMtuExpand()
        {
            var nextMtu = 0;

            if (_smallestFailedMtu < 0)
                nextMtu = Math.Min(ushort.MaxValue, (int) (Mtu * 1.25));
            else
                nextMtu = (int) ((_smallestFailedMtu + (float) Mtu) / 2.0f);

            if (nextMtu > Parent.Configuration.LimitMtu)
                nextMtu = Parent.Configuration.LimitMtu;

            if (nextMtu == Mtu)
            {
                FixMtu();
                return;
            }

            _lastMtuExpandSent = DateTime.UtcNow;
            int size = nextMtu - Datagram.GetHeaderSize(false);
            Datagram mtuDatagram =
                Parent.CreateDatagram(MessageType.ExpandMTURequest, _serviceUnreliableChannel.Descriptor, size);
            mtuDatagram.Length = size;
            if (mtuDatagram.GetTotalSize() != nextMtu)
                throw new Exception(
                    "Datagram total size doesn't match header+body size. Perhaps header size calculation failed");

            _logger.Debug($"#{Id} expanding MTU to {nextMtu}...");
            _serviceUnreliableChannel.SendDatagramAsync(mtuDatagram, this.CancellationToken).ContinueWith(t =>
            {
                Exception ex = t.Exception.GetInnermostException();
                var sex = ex as SocketException;
                _logger.Debug($"#{Id} MTU {nextMtu} expand failed ({ex.Message})");
                if (sex != null && sex.SocketErrorCode == SocketError.MessageSize)
                {
                    if (_smallestFailedMtu < 1 || nextMtu < _smallestFailedMtu)
                    {
                        _smallestFailedMtu = nextMtu;
                        _mtuFailedAttempts++;
                        if (_mtuFailedAttempts >= _mtuExpandMaxFailAttempts)
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
            if (_mtuStatus != MtuExpansionStatus.Started)
                return;

            if ((DateTime.UtcNow - _lastMtuExpandSent).TotalMilliseconds > _mtuExpandFrequency)
            {
                _mtuFailedAttempts++;
                if (_mtuFailedAttempts >= _mtuExpandMaxFailAttempts)
                {
                    FixMtu();
                    return;
                }

                SendNextMtuExpand();
            }
        }

        enum MtuExpansionStatus
        {
            NotStarted = 0,
            Started = 1,
            Finished = 2
        }
    }
}