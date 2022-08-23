using System;
using System.IO;

namespace Neon.Networking.IO
{
#if NETSTANDARD2_1 || NETCOREAPP3_1
    static class SpanExtensions
    {
        public static void Read(this ReadOnlySpan<byte> span, ref int position, byte[] array, int index, int count)
        {
            span.Slice(position).CopyTo(array.AsSpan(index, count));
            position += count;
        }

        public static ReadOnlySpan<byte> ReadSpan(this ReadOnlySpan<byte> span, ref int position, int count)
        {
            var result = span.Slice(position, count);
            position += count;
            return result;
        }

        public static bool ReadBoolean(this ReadOnlySpan<byte> span, ref int position)
        {
            byte result = span[position];
            position += 1;
            return result == 1;
        }

        public static unsafe float ReadSingle(this ReadOnlySpan<byte> span, ref int position)
        {
            uint tmpBuffer = (uint)(span[position] | span[position + 1] << 8 | span[position + 2] << 16 | span[position + 3] << 24);
            var result = *((float*)&tmpBuffer);
            position += 4;
            return result;
        }

        public static unsafe double ReadDouble(this ReadOnlySpan<byte> span, ref int position)
        {
            uint lo = (uint)(span[position] | span[position + 1] << 8 |
                span[position + 2] << 16 | span[position + 3] << 24);
            uint hi = (uint)(span[position + 4] | span[position + 5] << 8 |
                span[position + 6] << 16 | span[position + 7] << 24);

            ulong tmpBuffer = ((ulong)hi) << 32 | lo;
            var result = *((double*)&tmpBuffer);
            position += 8;
            return result;
        }

        public static sbyte ReadSByte(this ReadOnlySpan<byte> span, ref int position)
        {
            var result = (sbyte)span[position];
            position++;
            return result;
        }

        public static byte ReadByte(this ReadOnlySpan<byte> span, ref int position)
        {
            byte result = span[position];
            position += 1;
            return result;
        }

        public static short ReadInt16(this ReadOnlySpan<byte> span, ref int position)
        {
            var result = (short)(span[position] | span[position + 1] << 8);
            position += 2;
            return result;
        }

        public static ushort ReadUInt16(this ReadOnlySpan<byte> span, ref int position)
        {
            var result = (ushort)(span[position] | span[position + 1] << 8);
            position += 2;
            return result;
        }

        public static ulong ReadUInt64(this ReadOnlySpan<byte> span, ref int position)
        {
            uint lo = (uint)(span[position] | span[position + 1] << 8 |
                 span[position + 2] << 16 | span[position + 3] << 24);
            uint hi = (uint)(span[position + 4] | span[position + 5] << 8 |
                             span[position + 6] << 16 | span[position + 7] << 24);
            var result = ((ulong)hi) << 32 | lo;
            position += 8;
            return result;
        }

        public static byte[] ReadBytes(this ReadOnlySpan<byte> span, ref int position, int count)
        {
            byte[] result = new byte[count];
            span.Slice(position, count).CopyTo(result);
            position += count;
            return result;
        }

        public static int ReadInt32(this ReadOnlySpan<byte> span, ref int position)
        {
            int result = span[position] | span[position + 1] << 8 | span[position + 2] << 16 | span[position + 3] << 24;
            position += 4;
            return result;
        }

        public static uint ReadUInt32(this ReadOnlySpan<byte> span, ref int position)
        {
            uint result = (uint)(span[position] | span[position + 1] << 8 | span[position + 2] << 16 | span[position + 3] << 24);
            position += 4;
            return result;
        }

        public static long ReadInt64(this ReadOnlySpan<byte> span, ref int position)
        {
            uint lo = (uint)(span[position] | span[position + 1] << 8 |
                span[position + 2] << 16 | span[position + 3] << 24);
            uint hi = (uint)(span[position + 4] | span[position + 5] << 8 |
                             span[position + 6] << 16 | span[position + 7] << 24);
            long result = (long)((ulong)hi) << 32 | lo;
            position += 8;
            return result;
        }

        public static unsafe string ReadString(this ReadOnlySpan<byte> span, ref int position, System.Text.Encoding encoding)
        {
            int stringLength = span.Read7BitEncodedInt(ref position);
            if (stringLength < 0)
            {
                throw new IOException("Invalid length of string");
            }

            if (stringLength == 0)
            {
                return String.Empty;
            }

            var stringSpan = span.Slice(position, stringLength);
            string result = null;
            fixed (byte* bytes = &stringSpan.GetPinnableReference())
            {
                result = encoding.GetString(bytes, stringLength);
            }

            position += stringLength;
            return result;
        }

        static int Read7BitEncodedInt(this ReadOnlySpan<byte> span, ref int position)
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
                b = span.ReadByte(ref position);
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }
    }
#endif
}
