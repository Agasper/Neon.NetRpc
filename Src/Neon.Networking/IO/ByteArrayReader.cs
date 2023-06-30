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
            get => _position;
            set
            {
                CheckPosition(value);
                _position = value;
            }
        }

        public byte[] Buffer { get; }

        public int Offset { get; }

        public int Count { get; }

        int _position;
        readonly Encoding _encoding;

        public ByteArrayReader(ArraySegment<byte> segment, Encoding encoding) : this(segment.Array, segment.Offset,
            segment.Count, encoding)
        {
        }

        public ByteArrayReader(ArraySegment<byte> segment) : this(segment.Array, segment.Offset, segment.Count,
            Encoding.Default)
        {
        }

        public ByteArrayReader(byte[] array, int offset, int count) : this(array, offset, count, Encoding.Default)
        {
            _encoding = Encoding.UTF8;
        }

        public ByteArrayReader(byte[] array, int offset, int count, Encoding encoding)
        {
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            Buffer = array;
            Offset = offset;
            Count = count;
            Position = 0;
            _encoding = encoding;
        }

        void CheckPosition()
        {
            CheckPosition(_position);
        }

        void CheckPosition(int newPosition)
        {
            if (newPosition > Count)
                throw new IndexOutOfRangeException("EOF");
            if (newPosition < 0)
                throw new IndexOutOfRangeException("Position can not be negative");
        }

        public ArraySegment<byte> ReadArraySegment(int count)
        {
            CheckPosition(_position + count);
            var result = new ArraySegment<byte>(Buffer, _position, count);
            _position += count;
            return result;
        }

        public void Read(byte[] array, int index, int count)
        {
            CheckPosition(_position + count);
            Array.Copy(Buffer, _position, array, index, count);
            _position += count;
        }

        public unsafe float ReadSingle()
        {
            CheckPosition(_position + 4);
            var tmpBuffer = (uint) (Buffer[_position] | (Buffer[_position + 1] << 8) | (Buffer[_position + 2] << 16) |
                                    (Buffer[_position + 3] << 24));
            float result = *(float*) &tmpBuffer;
            _position += 4;
            return result;
        }

        public unsafe double ReadDouble()
        {
            CheckPosition(_position + 8);
            var lo = (uint) (Buffer[_position] | (Buffer[_position + 1] << 8) |
                             (Buffer[_position + 2] << 16) | (Buffer[_position + 3] << 24));
            var hi = (uint) (Buffer[_position + 4] | (Buffer[_position + 5] << 8) |
                             (Buffer[_position + 6] << 16) | (Buffer[_position + 7] << 24));

            ulong tmpBuffer = ((ulong) hi << 32) | lo;
            double result = *(double*) &tmpBuffer;
            _position += 8;
            return result;
        }

        public bool ReadBoolean()
        {
            CheckPosition(_position + 1);
            bool result = Buffer[_position] != 0;
            _position++;
            return result;
        }

        public byte ReadByte()
        {
            CheckPosition(_position + 1);
            byte result = Buffer[_position];
            _position++;
            return result;
        }

        public byte[] ReadBytes(int count)
        {
            CheckPosition(_position + count);
            var result = new byte[count];
            Array.Copy(Buffer, _position, result, 0, count);
            _position += count;
            return result;
        }

        public short ReadInt16()
        {
            CheckPosition(_position + 2);
            var result = (short) (Buffer[_position] | (Buffer[_position + 1] << 8));
            _position += 2;
            return result;
        }

        public int ReadInt32()
        {
            CheckPosition(_position + 4);
            int result = Buffer[_position] | (Buffer[_position + 1] << 8) | (Buffer[_position + 2] << 16) |
                         (Buffer[_position + 3] << 24);
            _position += 4;
            return result;
        }

        public long ReadInt64()
        {
            CheckPosition(_position + 8);
            var lo = (uint) (Buffer[_position] | (Buffer[_position + 1] << 8) |
                             (Buffer[_position + 2] << 16) | (Buffer[_position + 3] << 24));
            var hi = (uint) (Buffer[_position + 4] | (Buffer[_position + 5] << 8) |
                             (Buffer[_position + 6] << 16) | (Buffer[_position + 7] << 24));
            long result = ((long) hi << 32) | lo;
            _position += 8;
            return result;
        }

        public sbyte ReadSByte()
        {
            CheckPosition(_position + 1);
            var result = (sbyte) Buffer[_position];
            _position++;
            return result;
        }

        int Read7BitEncodedInt()
        {
            // Read out an Int32 7 bits at a time.  The high bit
            // of the byte when on means to continue reading more bytes.
            var count = 0;
            var shift = 0;
            byte b;
            do
            {
                // Check for a corrupted stream.  Read a max of 5 bytes.
                // In a future version, add a DataFormatException.
                if (shift == 5 * 7) // 5 bytes max per Int32, shift += 7
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
            if (stringLengthBytes < 0) throw new IOException("Invalid length of string");

            if (stringLengthBytes == 0) return string.Empty;

            CheckPosition(_position + stringLengthBytes);

            string result = _encoding.GetString(Buffer, _position, stringLengthBytes);
            _position += stringLengthBytes;
            return result;
        }

        public ushort ReadUInt16()
        {
            CheckPosition(_position + 2);
            var result = (ushort) (Buffer[_position] | (Buffer[_position + 1] << 8));
            _position += 2;
            return result;
        }

        public uint ReadUInt32()
        {
            CheckPosition(_position + 4);
            var result = (uint) (Buffer[_position] | (Buffer[_position + 1] << 8) | (Buffer[_position + 2] << 16) |
                                 (Buffer[_position + 3] << 24));
            _position += 4;
            return result;
        }

        public ulong ReadUInt64()
        {
            CheckPosition(_position + 8);
            var lo = (uint) (Buffer[_position] | (Buffer[_position + 1] << 8) |
                             (Buffer[_position + 2] << 16) | (Buffer[_position + 3] << 24));
            var hi = (uint) (Buffer[_position + 4] | (Buffer[_position + 5] << 8) |
                             (Buffer[_position + 6] << 16) | (Buffer[_position + 7] << 24));
            ulong result = ((ulong) hi << 32) | lo;
            _position += 8;
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