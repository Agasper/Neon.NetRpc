using System;
using System.IO;
using System.Text;

namespace Neon.Networking.IO
{
    class ByteArrayReader : IByteReader
    {
        public bool EOF => Position >= Count;
        public int Position
        {
            get
            {
                return position;
            }
            set
            {
                CheckPosition(value);
                position = value;
            }
        }
        public byte[] Buffer => buffer;
        public int Offset => offset;
        public int Count => count;

        int position;
        byte[] buffer;
        int offset;
        int count;
        Encoding encoding;

        public ByteArrayReader(ArraySegment<byte> segment, Encoding encoding) : this(segment.Array, segment.Offset, segment.Count, encoding)
        { }

        public ByteArrayReader(ArraySegment<byte> segment) : this(segment.Array, segment.Offset, segment.Count, UTF8Encoding.Default)
        { }

        public ByteArrayReader(byte[] array, int offset, int count) : this(array, offset, count, UTF8Encoding.Default)
        {
            this.encoding = System.Text.Encoding.UTF8;
        }

        public ByteArrayReader(byte[] array, int offset, int count, Encoding encoding)
        {
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            this.buffer = array;
            this.offset = offset;
            this.count = count;
            this.Position = 0;
            this.encoding = encoding;
        }

        void CheckPosition()
        {
            CheckPosition(position);
        }

        void CheckPosition(int newPosition)
        {
            if (newPosition > count)
                throw new IndexOutOfRangeException("EOF");
            if (newPosition < 0)
                throw new IndexOutOfRangeException("Position can not be negative");
        }

        public ArraySegment<byte> ReadArraySegment(int count)
        {
            CheckPosition(position + count);
            var result = new ArraySegment<byte>(buffer, position, count);
            position += count;
            return result;
        }

        public void Read(byte[] array, int index, int count)
        {
            CheckPosition(position + count);
            Array.Copy(buffer, position, array, index, count);
            position += count;
        }

        public unsafe float ReadSingle()
        {
            CheckPosition(position + 4);
            uint tmpBuffer = (uint)(buffer[position] | buffer[position + 1] << 8 | buffer[position + 2] << 16 | buffer[position + 3] << 24);
            var result = *((float*)&tmpBuffer);
            position += 4;
            return result;
        }

        public unsafe double ReadDouble()
        {
            CheckPosition(position + 8);
            uint lo = (uint)(buffer[position] | buffer[position + 1] << 8 |
                buffer[position + 2] << 16 | buffer[position + 3] << 24);
            uint hi = (uint)(buffer[position + 4] | buffer[position + 5] << 8 |
                buffer[position + 6] << 16 | buffer[position + 7] << 24);

            ulong tmpBuffer = ((ulong)hi) << 32 | lo;
            var result = *((double*)&tmpBuffer);
            position += 8;
            return result;
        }

        public bool ReadBoolean()
        {
            CheckPosition(position + 1);
            var result = buffer[position] != 0;
            position++;
            return result;
        }

        public byte ReadByte()
        {
            CheckPosition(position + 1);
            var result = buffer[position];
            position++;
            return result;
        }

        public byte[] ReadBytes(int count)
        {
            CheckPosition(position + count);
            byte[] result = new byte[count];
            Array.Copy(buffer, position, result, 0, count);
            position += count;
            return result;
        }

        public short ReadInt16()
        {
            CheckPosition(position + 2);
            var result = (short)(buffer[position] | buffer[position + 1] << 8);
            position += 2;
            return result;
        }

        public int ReadInt32()
        {
            CheckPosition(position + 4);
            var result = (int)(buffer[position] | buffer[position + 1] << 8 | buffer[position + 2] << 16 | buffer[position + 3] << 24);
            position += 4;
            return result;
        }

        public long ReadInt64()
        {
            CheckPosition(position + 8);
            uint lo = (uint)(buffer[position] | buffer[position + 1] << 8 |
                 buffer[position + 2] << 16 | buffer[position + 3] << 24);
            uint hi = (uint)(buffer[position + 4] | buffer[position + 5] << 8 |
                             buffer[position + 6] << 16 | buffer[position + 7] << 24);
            var result = (long)((ulong)hi) << 32 | lo;
            position += 8;
            return result;
        }

        public sbyte ReadSByte()
        {
            CheckPosition(position + 1);
            var result = (sbyte)buffer[position];
            position++;
            return result;
        }

        int Read7BitEncodedInt()
        {
            // Read out an Int32 7 bits at a time.  The high bit
            // of the byte when on means to continue reading more bytes.
            int count = 0;
            int shift = 0;
            byte b;
            do
            {
                // Check for a corrupted stream.  Read a max of 5 bytes.
                // In a future version, add a DataFormatException.
                if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
                    throw new FormatException("Bad 7Bits encoded integer format");

                // ReadByte handles end of stream cases for us.
                b = ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }

        public string ReadString()
        {
            int stringLengthBytes = Read7BitEncodedInt();
            if (stringLengthBytes < 0)
            {
                throw new IOException("Invalid length of string");
            }

            if (stringLengthBytes == 0)
            {
                return String.Empty;
            }

            CheckPosition(position + stringLengthBytes);

            string result = encoding.GetString(buffer, position, stringLengthBytes);
            position += stringLengthBytes;
            return result;
        }

        public ushort ReadUInt16()
        {
            CheckPosition(position + 2);
            var result = (ushort)(buffer[position] | buffer[position + 1] << 8);
            position += 2;
            return result;
        }

        public uint ReadUInt32()
        {
            CheckPosition(position + 4);
            var result = (uint)(buffer[position] | buffer[position + 1] << 8 | buffer[position + 2] << 16 | buffer[position + 3] << 24);
            position += 4;
            return result;
        }

        public ulong ReadUInt64()
        {
            CheckPosition(position + 8);
            uint lo = (uint)(buffer[position] | buffer[position + 1] << 8 |
                             buffer[position + 2] << 16 | buffer[position + 3] << 24);
            uint hi = (uint)(buffer[position + 4] | buffer[position + 5] << 8 |
                             buffer[position + 6] << 16 | buffer[position + 7] << 24);
            var result = ((ulong)hi) << 32 | lo;
            position += 8;
            return result;
        }

#if NETCOREAPP3_1
        public ReadOnlyMemory<byte> ReadMemory(int count)
        {
            CheckPosition(position + count);
            var result = buffer.AsMemory(position, count);
            position += count;
            return result;
        }

        public ReadOnlySpan<byte> ReadSpan(int count)
        {
            CheckPosition(position + count);
            var result = buffer.AsSpan(position, count);
            position += count;
            return result;
        }
#endif

        public int ReadVarInt32()
        {
            return VarintBitConverter.ToInt32(this);
        }

        public long ReadVarInt64()
        {
            return VarintBitConverter.ToInt64(this);
        }

        public uint ReadVarUInt32()
        {
            return VarintBitConverter.ToUInt32(this);
        }

        public ulong ReadVarUInt64()
        {
            return VarintBitConverter.ToUInt64(this);
        }
    }
}