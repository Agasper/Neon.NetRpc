using Neon.Networking.Messages;

namespace Neon.Networking.Udp.Messages
{
    struct UdpMessageHeader
    {
        public MessageFlagsEnum Flags { get; set; }

        public UdpMessageHeader( MessageFlagsEnum flags)
        {
            Flags = flags;
        }
        
        public static UdpMessageHeader ReadFromDatagram(Datagram datagram)
        {
            datagram.Position = 0;
            return new UdpMessageHeader((MessageFlagsEnum)datagram.ReadVarInt32());
        }

        public void WriteToDatagram(Datagram datagram)
        {
            datagram.WriteVarInt((int)Flags);
        }

        public override string ToString()
        {
            return $"{nameof(UdpMessageHeader)}[flags={Flags}]";
        }
    }
}