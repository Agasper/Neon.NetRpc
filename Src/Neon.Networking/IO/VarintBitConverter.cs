namespace Neon.Networking.IO
{
    public interface IByteReader
    {
        byte ReadByte();
    }

    public interface IByteWriter
    {
        void Write(byte value);
    }

    class VarintBitConverter
    {
        /// <summary>
        ///     Returns the specified byte value as varint encoded array of bytes.
        /// </summary>
        /// <param name="writer">A byte writer</param>
        /// <param name="value">Byte value</param>
        /// <returns>Varint array of bytes.</returns>
        public static void WriteVarintBytes(IByteWriter writer, byte value)
        {
            WriteVarintBytes(writer, (ulong) value);
        }

        /// <summary>
        ///     Returns the specified 16-bit signed value as varint encoded array of bytes.
        /// </summary>
        /// <param name="writer">A byte writer</param>
        /// <param name="value">16-bit signed value</param>
        /// <returns>Varint array of bytes.</returns>
        public static void WriteVarintBytes(IByteWriter writer, short value)
        {
            long zigzag = EncodeZigZag(value, 16);
            WriteVarintBytes(writer, (ulong) zigzag);
        }

        /// <summary>
        ///     Returns the specified 16-bit unsigned value as varint encoded array of bytes.
        /// </summary>
        /// <param name="writer">A byte writer</param>
        /// <param name="value">16-bit unsigned value</param>
        /// <returns>Varint array of bytes.</returns>
        public static void WriteVarintBytes(IByteWriter writer, ushort value)
        {
            WriteVarintBytes(writer, (ulong) value);
        }

        /// <summary>
        ///     Returns the specified 32-bit signed value as varint encoded array of bytes.
        /// </summary>
        /// <param name="writer">A byte writer</param>
        /// <param name="value">32-bit signed value</param>
        /// <returns>Varint array of bytes.</returns>
        public static void WriteVarintBytes(IByteWriter writer, int value)
        {
            long zigzag = EncodeZigZag(value, 32);
            WriteVarintBytes(writer, (ulong) zigzag);
        }

        /// <summary>
        ///     Returns the specified 32-bit unsigned value as varint encoded array of bytes.
        /// </summary>
        /// <param name="writer">A byte writer</param>
        /// <param name="value">32-bit unsigned value</param>
        /// <returns>Varint array of bytes.</returns>
        public static void WriteVarintBytes(IByteWriter writer, uint value)
        {
            WriteVarintBytes(writer, (ulong) value);
        }

        public static int CalculateVarintBytes(uint value)
        {
            return CalcVarintBytes(value);
        }

        /// <summary>
        ///     Returns the specified 64-bit signed value as varint encoded array of bytes.
        /// </summary>
        /// <param name="writer">A byte writer</param>
        /// <param name="value">64-bit signed value</param>
        /// <returns>Varint array of bytes.</returns>
        public static void WriteVarintBytes(IByteWriter writer, long value)
        {
            long zigzag = EncodeZigZag(value, 64);
            WriteVarintBytes(writer, (ulong) zigzag);
        }

        public static int CalculateVarintBytes(long value)
        {
            long zigzag = EncodeZigZag(value, 64);
            return CalcVarintBytes((ulong) zigzag);
        }

        /// <summary>
        ///     Returns the specified 64-bit unsigned value as varint encoded array of bytes.
        /// </summary>
        /// <param name="writer">A byte writer</param>
        /// <param name="value">64-bit unsigned value</param>
        /// <returns>Varint array of bytes.</returns>
        public static void WriteVarintBytes(IByteWriter writer, ulong value)
        {
            do
            {
                ulong byteVal = value & 0x7f;
                value >>= 7;

                if (value != 0) byteVal |= 0x80;

                writer.Write((byte) byteVal);
            } while (value != 0);
        }

        public static int CalcVarintBytes(ulong value)
        {
            var cnt = 0;
            do
            {
                value >>= 7;
                cnt++;
            } while (value != 0);

            return cnt;
        }

        /// <summary>
        ///     Returns byte value from varint encoded array of bytes.
        /// </summary>
        /// <param name="reader">A byte reader</param>
        /// <returns>Byte value</returns>
        public static byte ToByte(IByteReader reader)
        {
            return (byte) ToTarget(reader, 8);
        }

        /// <summary>
        ///     Returns 16-bit signed value from varint encoded array of bytes.
        /// </summary>
        /// <param name="reader">A byte reader</param>
        /// <returns>16-bit signed value</returns>
        public static short ToInt16(IByteReader reader)
        {
            ulong zigzag = ToTarget(reader, 16);
            return (short) DecodeZigZag(zigzag);
        }

        /// <summary>
        ///     Returns 16-bit usigned value from varint encoded array of bytes.
        /// </summary>
        /// <param name="reader">A byte reader</param>
        /// <returns>16-bit usigned value</returns>
        public static ushort ToUInt16(IByteReader reader)
        {
            return (ushort) ToTarget(reader, 16);
        }

        /// <summary>
        ///     Returns 32-bit signed value from varint encoded array of bytes.
        /// </summary>
        /// <param name="reader">A byte reader</param>
        /// <returns>32-bit signed value</returns>
        public static int ToInt32(IByteReader reader)
        {
            ulong zigzag = ToTarget(reader, 32);
            return (int) DecodeZigZag(zigzag);
        }

        /// <summary>
        ///     Returns 32-bit unsigned value from varint encoded array of bytes.
        /// </summary>
        /// <param name="reader">A byte reader</param>
        /// <returns>32-bit unsigned value</returns>
        public static uint ToUInt32(IByteReader reader)
        {
            return (uint) ToTarget(reader, 32);
        }

        /// <summary>
        ///     Returns 64-bit signed value from varint encoded array of bytes.
        /// </summary>
        /// <param name="reader">A byte reader</param>
        /// <returns>64-bit signed value</returns>
        public static long ToInt64(IByteReader reader)
        {
            ulong zigzag = ToTarget(reader, 64);
            return DecodeZigZag(zigzag);
        }

        /// <summary>
        ///     Returns 64-bit unsigned value from varint encoded array of bytes.
        /// </summary>
        /// <param name="reader">A byte reader</param>
        /// <returns>64-bit unsigned value</returns>
        public static ulong ToUInt64(IByteReader reader)
        {
            return ToTarget(reader, 64);
        }

        static long EncodeZigZag(long value, int bitLength)
        {
            return (value << 1) ^ (value >> (bitLength - 1));
        }

        static long DecodeZigZag(ulong value)
        {
            if ((value & 0x1) == 0x1) return -1 * ((long) (value >> 1) + 1);

            return (long) (value >> 1);
        }

        static ulong ToTarget(IByteReader reader, int sizeBites)
        {
            var shift = 0;
            ulong result = 0;

            while (true)
            {
                ulong byteValue = reader.ReadByte();

                ulong tmp = byteValue & 0x7f;
                result |= tmp << shift;

                if ((byteValue & 0x80) != 0x80) return result;

                shift += 7;

                if (shift > sizeBites)
                    break;
            }

            return result;
        }
    }
}