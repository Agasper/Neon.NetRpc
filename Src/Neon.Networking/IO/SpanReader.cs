namespace Neon.Networking.IO
{
#if NETSTANDARD2_1 || NETCOREAPP3_1
    //https://github.com/microsoft/referencesource/blob/master/mscorlib/system/io/binaryreader.cs
    internal ref struct SpanReader
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

        readonly ReadOnlySpan<byte> span;
        int position;

        public SpanReader(ReadOnlySpan<byte> span, int position = 0)
        {
            this.span = span;
            this.position = 0;
        }

        public ArraySegment<byte> GetArraySegment(int count)
        {
            throw new NotSupportedException();
        }

        public void Read(byte[] array, int index, int count)
        {
            span.Read(ref position, array, index, count);
        }

        public ReadOnlySpan<byte> ReadSpan(int count)
        {
            return span.ReadSpan(ref position, count);
        }

        public bool ReadBoolean()
        {
            return span.ReadBoolean(ref position);
        }

        public float ReadSingle()
        {
            return span.ReadSingle(ref position);
        }

        public double ReadDouble()
        {
            return span.ReadDouble(ref position);
        }

        public byte ReadByte()
        {
            return span.ReadByte(ref position);
        }

        public sbyte ReadSByte()
        {
            return span.ReadSByte(ref position);
        }

        public short ReadInt16()
        {
            return span.ReadInt16(ref position);
        }

        public ushort ReadUInt16()
        {
            return span.ReadUInt16(ref position);
        }

        public uint ReadUInt32()
        {
            return span.ReadUInt32(ref position);
        }

        public ulong ReadUInt64()
        {
            return span.ReadUInt64(ref position);
        }

        public byte[] ReadBytes(int count)
        {
            return span.ReadBytes(ref position, count);
        }

        public int ReadInt32()
        {
            return span.ReadInt32(ref position);
        }

        public long ReadInt64()
        {
            return span.ReadInt64(ref position);
        }

        public string ReadString()
        {
            return span.ReadString(ref position, Encoding.UTF8);
        }

    }
#endif
}