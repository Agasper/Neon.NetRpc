using System;

namespace Neon.Networking.Messages
{
    [Flags]
    enum MessageFlagsEnum : byte
    {
        None = 0,
        Compressed = 1,
        Encrypted = 2
        //4
        //8
        //16
        //32
    }
}