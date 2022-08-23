using System;
using Neon.Networking.Messages;

namespace Neon.Networking.Tcp.Messages
{
    class TcpMessageHeaderFactory
    {
        int sizeBitsPosition;
        bool flagsRead;
        bool sizeRead;

        TcpMessageHeader header;

        public TcpMessageHeaderFactory()
        {
            
        }

        public bool Write(ArraySegment<byte> newData, out int bytesRead, out TcpMessageHeader header)
        {
            bytesRead = 0;
            header = default;
            if (newData.Count == 0)
                return false;
            if (flagsRead && sizeRead)
            {
                header = this.header;
                return true;
            }

            for (int i = 0; i < newData.Count; i++)
            {
                try
                {
                    if (WriteByte(newData.Array[newData.Offset + i]))
                    {
                        header = this.header;
                        return true;
                    }
                }
                finally
                {
                    bytesRead = i + 1;
                }
            }

            return false;
        }


        bool WriteByte(byte value)
        {
            if (!flagsRead)
            {
                header.Flags = (TcpMessageFlagsEnum)((value >> 2) & 0b0000_0011);
                header.MessageType = (TcpMessageTypeEnum) (value & 0b0000_0011);
                flagsRead = true;
                return false;
            }

            uint byteValue = value;

            uint tmp = byteValue & 0x7f;
            header.MessageSize |= (int)(tmp << sizeBitsPosition);

            if ((byteValue & 0x80) != 0x80)
            {
                sizeRead = true;
                return true;
            }

            sizeBitsPosition += 7;

            if (sizeBitsPosition > 32) //int bits
                throw new InvalidOperationException("Message header size exceeded 32 bits value");

            return false;
        }

        public void Reset()
        {
            sizeBitsPosition = 0;
            header = default;
            flagsRead = false;
            sizeRead = false;
        }

        public static ArraySegment<byte> Build(TcpMessageHeader header)
        {
            byte[] rawHeader = new byte[5];
            int headerPos = 1;
            uint value = (uint)header.MessageSize;
            do
            {
                var byteVal = value & 0x7f;
                value >>= 7;

                if (value != 0)
                {
                    byteVal |= 0x80;
                }

                rawHeader[headerPos] = (byte)byteVal;
                headerPos++;

            } while (value != 0);

            rawHeader[0] = (byte)(((int)header.Flags << 2) | (int)header.MessageType);

            return new ArraySegment<byte>(rawHeader, 0, headerPos);
        }

    }
}
