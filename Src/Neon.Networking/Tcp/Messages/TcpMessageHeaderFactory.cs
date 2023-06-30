using System;

namespace Neon.Networking.Tcp.Messages
{
    class TcpMessageHeaderFactory
    {
        bool _flagsRead;
        int _sizeBitsPosition;
        bool _sizeRead;

        TcpMessageHeader header;

        public bool Write(ArraySegment<byte> newData, out int bytesRead, out TcpMessageHeader header)
        {
            bytesRead = 0;
            header = default;
            if (newData.Count == 0)
                return false;
            if (_flagsRead && _sizeRead)
            {
                header = this.header;
                return true;
            }

            for (var i = 0; i < newData.Count; i++)
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

            return false;
        }


        bool WriteByte(byte value)
        {
            if (!_flagsRead)
            {
                header.Flags = (TcpMessageFlagsEnum) ((value >> 2) & 0b0000_0011);
                header.MessageType = (TcpMessageTypeEnum) (value & 0b0000_0011);
                _flagsRead = true;
                return false;
            }

            uint byteValue = value;

            uint tmp = byteValue & 0x7f;
            header.MessageSize |= (int) (tmp << _sizeBitsPosition);

            if ((byteValue & 0x80) != 0x80)
            {
                _sizeRead = true;
                return true;
            }

            _sizeBitsPosition += 7;

            if (_sizeBitsPosition > 32) //int bits
                throw new InvalidOperationException("Message header size exceeded 32 bits value");

            return false;
        }

        public void Reset()
        {
            _sizeBitsPosition = 0;
            header = default;
            _flagsRead = false;
            _sizeRead = false;
        }

        public static ArraySegment<byte> Build(TcpMessageHeader header)
        {
            var rawHeader = new byte[5];
            var headerPos = 1;
            var value = (uint) header.MessageSize;
            do
            {
                uint byteVal = value & 0x7f;
                value >>= 7;

                if (value != 0) byteVal |= 0x80;

                rawHeader[headerPos] = (byte) byteVal;
                headerPos++;
            } while (value != 0);

            rawHeader[0] = (byte) (((int) header.Flags << 2) | (int) header.MessageType);

            return new ArraySegment<byte>(rawHeader, 0, headerPos);
        }
    }
}