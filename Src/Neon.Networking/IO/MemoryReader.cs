using System;
using System.IO;
using System.Text;

namespace Neon.Networking.IO
{
    // public class MemoryReader : IByteReader
    // {
    //     public bool EOF => Position >= memory.Length;
    //
    //     public int Position
    //     {
    //         get { return position; }
    //         set { position = value; }
    //     }
    //
    //     readonly ReadOnlyMemory<byte> memory;
    //     int position;
    //
    //     public MemoryReader(ReadOnlyMemory<byte> memory, int position = 0)
    //     {
    //         this.memory = memory;
    //         this.position = 0;
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
    //         memory.Span.Read(ref position, array, index, count);
    //     }
    //
    //     public ReadOnlySpan<byte> ReadSpan(int count)
    //     {
    //         return memory.Span.ReadSpan(ref position, count);
    //     }
    //
    //     public bool ReadBoolean()
    //     {
    //         return memory.Span.ReadBoolean(ref position);
    //     }
    //
    //     public float ReadSingle()
    //     {
    //         return memory.Span.ReadSingle(ref position);
    //     }
    //
    //     public double ReadDouble()
    //     {
    //         return memory.Span.ReadDouble(ref position);
    //     }
    //
    //     public byte ReadByte()
    //     {
    //         return memory.Span.ReadByte(ref position);
    //     }
    //
    //     public sbyte ReadSByte()
    //     {
    //         return memory.Span.ReadSByte(ref position);
    //     }
    //
    //     public short ReadInt16()
    //     {
    //         return memory.Span.ReadInt16(ref position);
    //     }
    //
    //     public ushort ReadUInt16()
    //     {
    //         return memory.Span.ReadUInt16(ref position);
    //     }
    //
    //     public ulong ReadUInt64()
    //     {
    //         return memory.Span.ReadUInt64(ref position);
    //     }
    //
    //     public byte[] ReadBytes(int count)
    //     {
    //         return memory.Span.ReadBytes(ref position, count);
    //     }
    //
    //     public int ReadInt32()
    //     {
    //         return memory.Span.ReadInt32(ref position);
    //     }
    //
    //     public long ReadInt64()
    //     {
    //         return memory.Span.ReadInt64(ref position);
    //     }
    //
    //     public string ReadString()
    //     {
    //         return memory.Span.ReadString(ref position, Encoding.UTF8);
    //     }
    //
    //     public ReadOnlyMemory<byte> ReadMemory(int count)
    //     {
    //         var result = memory.Slice(position, count);
    //         position += count;
    //         return result;
    //     }
    //
    //     public uint ReadUInt32()
    //     {
    //         return memory.Span.ReadUInt32(ref position);
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