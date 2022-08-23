using System;
using System.IO;

namespace Neon.Networking.Messages
{
    public interface IRawMessage : IDisposable
    {
        Guid Guid { get; }
        int Position { get; set; }
        int Length { get; set; }
        
        Stream AsStream();
        
        string ReadString();
        bool ReadBoolean();
        float ReadSingle();
        double ReadDouble();

        byte ReadByte();
        sbyte ReadSByte();
        short ReadInt16();
        ushort ReadUInt16();
        int ReadInt32();
        uint ReadUInt32();
        long ReadInt64();
        ulong ReadUInt64();
        decimal ReadDecimal();

        int ReadVarInt32();
        uint ReadVarUInt32();
        long ReadVarInt64();
        ulong ReadVarUInt64();

        byte[] ReadBytes(int count);

        int Write(string value);
        void Write(float value);
        void Write(double value);
        void Write(bool value);
        void Write(byte value);
        void Write(sbyte value);
        void Write(short value);
        void Write(ushort value);
        void Write(int value);
        void Write(uint value);
        void Write(long value);
        void Write(ulong value);
        void Write(decimal value);
        void Write(byte[] value);
        void Write(byte[] value, int index, int count);

        void WriteVarInt(int value);
        void WriteVarInt(uint value);
        void WriteVarInt(long value);
        void WriteVarInt(ulong value);
    }
}
