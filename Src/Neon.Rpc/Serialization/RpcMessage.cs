using System;
using System.Buffers;
using System.IO;
using Neon.Networking.Messages;

namespace Neon.Rpc.Serialization
{
    public interface IRpcMessage : IRawMessage
    {
        object ReadObject();
        void WriteObject(object obj);
    }
    
    public class RpcMessage : IRpcMessage
    {
        public Guid Guid => message.Guid;
        public int Length
        {
            get => message.Length;
            set => message.Length = value;
        }
        public int Position
        {
            get => message.Position;
            set => message.Position = value;
        }
        
        readonly RpcSerializer serializer;
        readonly RawMessage message;

        public RpcMessage(RpcSerializer serializer, RawMessage message)
        {
            this.serializer = serializer;
            this.message = message;
        }
        
        public static explicit operator RawMessage(RpcMessage msg) => msg.message;

        public void Dispose()
        {
            this.message.Dispose();
        }

        public object ReadObject()
        {
            return this.serializer.ParseBinary(this);
        }

        public void WriteObject(object obj)
        {
            this.serializer.WriteBinary(this, obj);
        }


        public Stream AsStream()
        {
            return this.message.AsStream();
        }

        public ReadOnlySequence<byte> GetReadOnlySequence()
        {
            return message.GetReadOnlySequence();
        }

        public string ReadString()
        {
            return this.message.ReadString();
        }

        public bool ReadBoolean()
        {
            return this.message.ReadBoolean();
        }

        public float ReadSingle()
        {
            return this.message.ReadSingle();
        }

        public double ReadDouble()
        {
            return this.message.ReadDouble();
        }

        public byte ReadByte()
        {
            return this.message.ReadByte();
        }

        public sbyte ReadSByte()
        {
            return this.message.ReadSByte();
        }

        public short ReadInt16()
        {
            return this.message.ReadInt16();
        }

        public ushort ReadUInt16()
        {
            return this.message.ReadUInt16();
        }

        public int ReadInt32()
        {
            return this.message.ReadInt32();
        }

        public uint ReadUInt32()
        {
            return this.message.ReadUInt32();
        }

        public long ReadInt64()
        {
            return this.message.ReadInt64();
        }

        public ulong ReadUInt64()
        {
            return this.message.ReadUInt64();
        }

        public decimal ReadDecimal()
        {
            return this.message.ReadDecimal();
        }

        public int ReadVarInt32()
        {
            return this.message.ReadVarInt32();
        }

        public uint ReadVarUInt32()
        {
            return this.message.ReadVarUInt32();
        }

        public long ReadVarInt64()
        {
            return this.message.ReadVarInt64();
        }

        public ulong ReadVarUInt64()
        {
            return this.message.ReadVarUInt64();
        }

        public byte[] ReadBytes(int count)
        {
            return this.message.ReadBytes(count);
        }

        public int Write(string value)
        {
            return this.message.Write(value);
        }

        public void Write(float value)
        {
            this.message.Write(value);
        }

        public void Write(double value)
        {
            this.message.Write(value);
        }

        public void Write(bool value)
        {
            this.message.Write(value);
        }

        public void Write(byte value)
        {
            this.message.Write(value);
        }

        public void Write(sbyte value)
        {
            this.message.Write(value);
        }

        public void Write(short value)
        {
            this.message.Write(value);
        }

        public void Write(ushort value)
        {
            this.message.Write(value);
        }

        public void Write(int value)
        {
            this.message.Write(value);
        }

        public void Write(uint value)
        {
            this.message.Write(value);
        }

        public void Write(long value)
        {
            this.message.Write(value);
        }

        public void Write(ulong value)
        {
            this.message.Write(value);
        }

        public void Write(decimal value)
        {
            this.message.Write(value);
        }

        public void Write(byte[] value)
        {
            this.message.Write(value);
        }

        public void Write(byte[] value, int index, int count)
        {
            this.message.Write(value, index, count);
        }

        public void WriteVarInt(int value)
        {
            this.message.WriteVarInt(value);
        }

        public void WriteVarInt(uint value)
        {
            this.message.WriteVarInt(value);
        }

        public void WriteVarInt(long value)
        {
            this.message.WriteVarInt(value);
        }

        public void WriteVarInt(ulong value)
        {
            this.message.WriteVarInt(value);
        }
    }
}