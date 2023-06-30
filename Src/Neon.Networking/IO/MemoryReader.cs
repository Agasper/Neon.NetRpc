namespace Neon.Networking.IO
{
    // public class MemoryReader : IByteReader
    // {
    //     public bool EOF => Position >= _memory.Length;
    //
    //     public int Position
    //     {
    //         get { return _position; }
    //         set { _position = value; }
    //     }
    //
    //     readonly ReadOnlyMemory<byte> _memory;
    //     int _position;
    //
    //     public MemoryReader(ReadOnlyMemory<byte> memory, int position = 0)
    //     {
    //         this._memory = memory;
    //         this._position = 0;
    //     }
    //
    //     public Stream AsStream()
    //     {
    //         throw new NotSupportedException();
    //     }
    //
    //     public ArraySegment<byte> ReadArraySegment(int count)
    //     {
    //         throw new NotSupportedException();
    //     }
    //
    //     public void Read(byte[] array, int index, int count)
    //     {
    //         _memory.Span.Read(ref _position, array, index, count);
    //     }
    //
    //     public ReadOnlySpan<byte> ReadSpan(int count)
    //     {
    //         return _memory.Span.ReadSpan(ref _position, count);
    //     }
    //
    //     public bool ReadBoolean()
    //     {
    //         return _memory.Span.ReadBoolean(ref _position);
    //     }
    //
    //     public float ReadSingle()
    //     {
    //         return _memory.Span.ReadSingle(ref _position);
    //     }
    //
    //     public double ReadDouble()
    //     {
    //         return _memory.Span.ReadDouble(ref _position);
    //     }
    //
    //     public byte ReadByte()
    //     {
    //         return _memory.Span.ReadByte(ref _position);
    //     }
    //
    //     public sbyte ReadSByte()
    //     {
    //         return _memory.Span.ReadSByte(ref _position);
    //     }
    //
    //     public short ReadInt16()
    //     {
    //         return _memory.Span.ReadInt16(ref _position);
    //     }
    //
    //     public ushort ReadUInt16()
    //     {
    //         return _memory.Span.ReadUInt16(ref _position);
    //     }
    //
    //     public ulong ReadUInt64()
    //     {
    //         return _memory.Span.ReadUInt64(ref _position);
    //     }
    //
    //     public byte[] ReadBytes(int count)
    //     {
    //         return _memory.Span.ReadBytes(ref _position, count);
    //     }
    //
    //     public int ReadInt32()
    //     {
    //         return _memory.Span.ReadInt32(ref _position);
    //     }
    //
    //     public long ReadInt64()
    //     {
    //         return _memory.Span.ReadInt64(ref _position);
    //     }
    //
    //     public string ReadString()
    //     {
    //         return _memory.Span.ReadString(ref _position, Encoding.UTF8);
    //     }
    //
    //     public ReadOnlyMemory<byte> ReadMemory(int count)
    //     {
    //         var result = _memory.Slice(_position, count);
    //         _position += count;
    //         return result;
    //     }
    //
    //     public uint ReadUInt32()
    //     {
    //         return _memory.Span.ReadUInt32(ref _position);
    //     }
    //
    //     public int ReadVarInt32()
    //     {
    //         return VarintBitConverter.ToInt32(this);
    //     }
    //
    //     public long ReadVarInt64()
    //     {
    //         return VarintBitConverter.ToInt64(this);
    //     }
    //
    //     public uint ReadVarUInt32()
    //     {
    //         return VarintBitConverter.ToUInt32(this);
    //     }
    //
    //     public ulong ReadVarUInt64()
    //     {
    //         return VarintBitConverter.ToUInt64(this);
    //     }
    // }
}