using System;

namespace Neon.Networking.Tcp.Messages
{
    [Flags]
    enum TcpMessageFlagsEnum : byte
    {
        None = 0,
        Compressed = 1,
        Encrypted = 2,
        //4
        //8
        //16
        //32
    }
}