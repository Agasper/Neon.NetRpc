using System;
using System.IO;
using System.Text;

namespace Neon.Networking.IO
{
    class ByteArrayWriter : IByteWriter
    {
        public int Position { get; set; }

        public int Length => Buffer.Length;

        public byte[] Buffer { get; }
        readonly int _count;
        readonly Encoding _encoding;
        readonly int _offset;

        public ByteArrayWriter(ArraySegment<byte> segment) : this(segment, Encoding.Default)
        {
        }

        public ByteArrayWriter(ArraySegment<byte> segment, Encoding encoding)
            : this(segment.Array, segment.Offset, segment.Count, encoding)
        {
        }

        public ByteArrayWriter(byte[] array, int offset, int count)
            : this(array, offset, count, Encoding.Default)
        {
        }

        public ByteArrayWriter(byte[] array, int offset, int count, Encoding encoding)
        {
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            Buffer = array;
            _offset = offset;
            _count = count;
            Position = 0;
            _encoding = encoding;
        }

        public void Write(byte value)
        {
            EnsureCapacity(1);
            Buffer[Position + _offset] = value;
            Position += 1;
        }

        public int GetBytesLeft()
        {
            return _count - (Position + _offset);
        }

        void EnsureCapacity(int count)
        {
            if (GetBytesLeft() < count)
                throw new IOException($"The provided {nameof(ArraySegment<byte>)} has no space left");
        }


        void Write7BitEncodedInt(int value)
        {
            // Write out an int 7 bits at a time.  The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            var v = (uint) value; // support negative numbers
            while (v >= 0x80)
            {
                Write((byte) (v | 0x80));
                v >>= 7;
            }

            Write((byte) v);
        }

        public int Write(string value)
        {
            int len = _encoding.GetByteCount(value);
            Write7BitEncodedInt(len);
            EnsureCapacity(len);
            _encoding.GetBytes(value, 0, value.Length, Buffer, Position + _offset);
            Position += len;
            return len;
        }

        public void Write(bool value)
        {
            EnsureCapacity(1);
            Write(value ? (byte) 1 : (byte) 0);
        }

        public void Write(sbyte value)
        {
            EnsureCapacity(1);
            Buffer[Position + _offset] = (byte) value;
            Position += 1;
        }

        public void Write(short value)
        {
            EnsureCapacity(2);
            Buffer[Position + _offset] = (byte) value;
            Buffer[Position + _offset + 1] = (byte) (value >> 8);
            Position += 2;
        }

        public void Write(ushort value)
        {
            EnsureCapacity(2);
            Buffer[Position + _offset] = (byte) value;
            Buffer[Position + _offset + 1] = (byte) (value >> 8);
            Position += 2;
        }

        public void Write(int value)
        {
            EnsureCapacity(4);
            Buffer[Position] = (byte) value;
            Buffer[Position + _offset + 1] = (byte) (value >> 8);
            Buffer[Position + _offset + 2] = (byte) (value >> 16);
            Buffer[Position + _offset + 3] = (byte) (value >> 24);
            Position += 4;
        }

        public void Write(uint value)
        {
            EnsureCapacity(4);
            Buffer[Position + _offset] = (byte) value;
            Buffer[Position + _offset + 1] = (byte) (value >> 8);
            Buffer[Position + _offset + 2] = (byte) (value >> 16);
            Buffer[Position + _offset + 3] = (byte) (value >> 24);
            Position += 4;
        }

        public void Write(long value)
        {
            EnsureCapacity(8);
            Buffer[Position + _offset + 0] = (byte) value;
            Buffer[Position + _offset + 1] = (byte) (value >> 8);
            Buffer[Position + _offset + 2] = (byte) (value >> 16);
            Buffer[Position + _offset + 3] = (byte) (value >> 24);
            Buffer[Position + _offset + 4] = (byte) (value >> 32);
            Buffer[Position + _offset + 5] = (byte) (value >> 40);
            Buffer[Position + _offset + 6] = (byte) (value >> 48);
            Buffer[Position + _offset + 7] = (byte) (value >> 56);
            Position += 8;
        }

        public void Write(ulong value)
        {
            EnsureCapacity(8);
            Buffer[Position + _offset + 0] = (byte) value;
            Buffer[Position + _offset + 1] = (byte) (value >> 8);
            Buffer[Position + _offset + 2] = (byte) (value >> 16);
            Buffer[Position + _offset + 3] = (byte) (value >> 24);
            Buffer[Position + _offset + 4] = (byte) (value >> 32);
            Buffer[Position + _offset + 5] = (byte) (value >> 40);
            Buffer[Position + _offset + 6] = (byte) (value >> 48);
            Buffer[Position + _offset + 7] = (byte) (value >> 56);
            Position += 8;
        }

        public unsafe void Write(float value)
        {
            uint TmpValue = *(uint*) &value;
            Buffer[Position + _offset + 0] = (byte) TmpValue;
            Buffer[Position + _offset + 1] = (byte) (TmpValue >> 8);
            Buffer[Position + _offset + 2] = (byte) (TmpValue >> 16);
            Buffer[Position + _offset + 3] = (byte) (TmpValue >> 24);
            Position += 4;
        }

        public unsafe void Write(double value)
        {
            ulong TmpValue = *(ulong*) &value;
            Buffer[Position + _offset + 0] = (byte) TmpValue;
            Buffer[Position + _offset + 1] = (byte) (TmpValue >> 8);
            Buffer[Position + _offset + 2] = (byte) (TmpValue >> 16);
            Buffer[Position + _offset + 3] = (byte) (TmpValue >> 24);
            Buffer[Position + _offset + 4] = (byte) (TmpValue >> 32);
            Buffer[Position + _offset + 5] = (byte) (TmpValue >> 40);
            Buffer[Position + _offset + 6] = (byte) (TmpValue >> 48);
            Buffer[Position + _offset + 7] = (byte) (TmpValue >> 56);
            Position += 8;
        }

        public void Write(byte[] value)
        {
            EnsureCapacity(value.Length);
            Array.Copy(value, 0, Buffer, Position + _offset, value.Length);
            Position += value.Length;
        }

        public void Write(byte[] value, int index, int count)
        {
            EnsureCapacity(count);
            Array.Copy(value, index, Buffer, Position + _offset, count);
            Position += count;
        }

        public void WriteVarInt(int value)
        {
            VarintBitConverter.WriteVarintBytes(this, value);
        }

        public void WriteVarInt(uint value)
        {
            VarintBitConverter.WriteVarintBytes(this, value);
        }

        public void WriteVarInt(long value)
        {
            VarintBitConverter.WriteVarintBytes(this, value);
        }

        public void WriteVarInt(ulong value)
        {
            VarintBitConverter.WriteVarintBytes(this, value);
        }

        //public void Dispose()
        //{
        //    if (bufferRented && buffer != null)
        //        ArrayPool<byte>.Shared.Return(buffer);
        //    buffer = null;
        //}
    }
}