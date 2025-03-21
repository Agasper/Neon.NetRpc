using System;
using Neon.Networking.Messages;

namespace Neon.Networking.Udp.Messages
{
    class UdpMessageInternal : IDisposable
    {
        public UdpMessage Message { get; }
        public MessageFlagsEnum Flags { get; }

        public UdpMessageInternal(UdpMessage message, MessageFlagsEnum flags)
        {
            Message = message;
            Flags = flags;
        }

        public void Dispose()
        {
            Message?.Dispose();
        }

        public override string ToString()
        {
            return $"{nameof(UdpMessageInternal)}[msg={Message},flags={Flags}]";
        }
    }
}