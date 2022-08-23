using System;
using System.IO;
using System.Text;

namespace Neon.Networking.IO
{
    class ByteArrayWriter : IByteWriter
    {
        public int Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }
        public int Length
        {
            get
            {
                return buffer.Length;
            }
        }
        public byte[] Buffer => buffer;

        int position;
        byte[] buffer;
        int offset;
        int count;
        Encoding encoding;

        public ByteArrayWriter(ArraySegment<byte> segment) : this(segment, UTF8Encoding.Default)
        {

        }

        public ByteArrayWriter(ArraySegment<byte> segment, Encoding encoding)
            : this(segment.Array, segment.Offset, segment.Count, encoding)
        {

        }

        public ByteArrayWriter(byte[] array, int offset, int count)
            : this(array, offset, count, UTF8Encoding.Default)
        {

        }

        public ByteArrayWriter(byte[] array, int offset, int count, Encoding encoding)
        {
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            this.buffer = array;
            this.offset = offset;
            this.count = count;
            this.position = 0;
            this.encoding = encoding;
        }

        public int GetBytesLeft()
        {
            return this.count - (position + offset);
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
            uint v = (uint)value;   // support negative numbers
            while (v >= 0x80)
            {
                Write((byte)(v | 0x80));
                v >>= 7;
            }
            Write((byte)v);
        }

        public int Write(string value)
        {
            int len = encoding.GetByteCount(value);
            Write7BitEncodedInt(len);
            EnsureCapacity(len);
            encoding.GetBytes(value, 0, value.Length, buffer, position+offset);
            position += len;
            return len;
        }

        public void Write(bool value)
        {
            EnsureCapacity(1);
            Write(value ? (byte)1 : (byte)0);
        }

        public void Write(byte value)
        {
            EnsureCapacity(1);
            buffer[position+offset] = value;
            position += 1;
        }

        public void Write(sbyte value)
        {
            EnsureCapacity(1);
            buffer[position + offset] = (byte)value;
            position += 1;
        }

        public void Write(short value)
        {
            EnsureCapacity(2);
            buffer[position + offset] = (byte)value;
            buffer[position + offset + 1] = (byte)(value >> 8);
            position += 2;
        }

        public void Write(ushort value)
        {
            EnsureCapacity(2);
            buffer[position + offset] = (byte)value;
            buffer[position + offset + 1] = (byte)(value >> 8);
            position += 2;
        }

        public void Write(int value)
        {
            EnsureCapacity(4);
            buffer[position] = (byte)value;
            buffer[position + offset + 1] = (byte)(value >> 8);
            buffer[position + offset + 2] = (byte)(value >> 16);
            buffer[position + offset + 3] = (byte)(value >> 24);
            position += 4;
        }

        public void Write(uint value)
        {
            EnsureCapacity(4);
            buffer[position + offset] = (byte)value;
            buffer[position + offset + 1] = (byte)(value >> 8);
            buffer[position + offset + 2] = (byte)(value >> 16);
            buffer[position + offset + 3] = (byte)(value >> 24);
            position += 4;
        }

        public void Write(long value)
        {
            EnsureCapacity(8);
            buffer[position + offset + 0] = (byte)value;
            buffer[position + offset + 1] = (byte)(value >> 8);
            buffer[position + offset + 2] = (byte)(value >> 16);
            buffer[position + offset + 3] = (byte)(value >> 24);
            buffer[position + offset + 4] = (byte)(value >> 32);
            buffer[position + offset + 5] = (byte)(value >> 40);
            buffer[position + offset + 6] = (byte)(value >> 48);
            buffer[position + offset + 7] = (byte)(value >> 56);
            position += 8;
        }

        public void Write(ulong value)
        {
            EnsureCapacity(8);
            buffer[position + offset + 0] = (byte)value;
            buffer[position + offset + 1] = (byte)(value >> 8);
            buffer[position + offset + 2] = (byte)(value >> 16);
            buffer[position + offset + 3] = (byte)(value >> 24);
            buffer[position + offset + 4] = (byte)(value >> 32);
            buffer[position + offset + 5] = (byte)(value >> 40);
            buffer[position + offset + 6] = (byte)(value >> 48);
            buffer[position + offset + 7] = (byte)(value >> 56);
            position += 8;
        }

        public unsafe void Write(float value)
        {
            uint TmpValue = *(uint*)&value;
            buffer[position + offset + 0] = (byte)TmpValue;
            buffer[position + offset + 1] = (byte)(TmpValue >> 8);
            buffer[position + offset + 2] = (byte)(TmpValue >> 16);
            buffer[position + offset + 3] = (byte)(TmpValue >> 24);
            position += 4;
        }

        public unsafe void Write(double value)
        {
            ulong TmpValue = *(ulong*)&value;
            buffer[position + offset + 0] = (byte)TmpValue;
            buffer[position + offset + 1] = (byte)(TmpValue >> 8);
            buffer[position + offset + 2] = (byte)(TmpValue >> 16);
            buffer[position + offset + 3] = (byte)(TmpValue >> 24);
            buffer[position + offset + 4] = (byte)(TmpValue >> 32);
            buffer[position + offset + 5] = (byte)(TmpValue >> 40);
            buffer[position + offset + 6] = (byte)(TmpValue >> 48);
            buffer[position + offset + 7] = (byte)(TmpValue >> 56);
            position += 8;
        }

        public void Write(byte[] value)
        {
            EnsureCapacity(value.Length);
            Array.Copy(value, 0, buffer, position + offset, value.Length);
            position += value.Length;
        }

        public void Write(byte[] value, int index, int count)
        {
            EnsureCapacity(count);
            Array.Copy(value, index, buffer, position + offset, count);
            position += count;
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