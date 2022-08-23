using System;
using System.Buffers;
using System.IO;
using System.Text;
using Microsoft.IO;
using Neon.Networking.IO;
using Neon.Util.Pooling;

namespace Neon.Networking.Messages
{
    public abstract class BaseRawMessage : IByteReader, IByteWriter, IRawMessage
    {
        public static readonly Encoding DEFAULT_ENCODING = Encoding.UTF8;
        
        public Guid Guid { get; protected set; }
        public int Position
        {
            get
            {
                CheckDisposed();
                if (stream == null)
                    return 0;
                return (int)stream.Position;
            }
            set
            {
                CheckDisposed();
                CheckStreamIsNotNull();
                stream.Position = value;
            }
        }

        public int Length
        {
            get
            {
                CheckDisposed();
                if (stream == null)
                    return 0;
                return (int) stream.Length;
            }
            set
            {
                CheckDisposed();
                CheckStreamIsNotNull();
                stream.SetLength(value);
            }
        }

#if DEBUG
        string disposeStack;
#endif
        
        protected RecyclableMemoryStream stream;
        protected BinaryReader reader;
        protected BinaryWriter writer;
        protected Encoding encoding;
        protected IMemoryManager memoryManager;

        protected bool disposed;
        
        public BaseRawMessage(IMemoryManager memoryManager, RecyclableMemoryStream stream)
            : this(memoryManager, stream, DEFAULT_ENCODING, Guid.Empty)
        {
            
        }

        public BaseRawMessage(IMemoryManager memoryManager, RecyclableMemoryStream stream, Encoding encoding)
        : this(memoryManager, stream, encoding, Guid.Empty)
        {
            
        }
        
        public BaseRawMessage(IMemoryManager memoryManager, RecyclableMemoryStream stream, Encoding encoding, Guid guid)
        {
            this.Guid = guid;
            this.stream = stream;
            this.memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
            this.encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            if (stream != null)
            {
                this.reader = new BinaryReader(stream, encoding, true);
                this.writer = new BinaryWriter(stream, encoding, true);
            }
        }

        public ReadOnlySequence<byte> GetReadOnlySequence()
        {
            CheckDisposed();
            if (stream == null)
                return new ReadOnlySequence<byte>();
            return stream.GetReadOnlySequence();
        }

        public Stream AsStream()
        {
            CheckDisposed();
            if (stream == null)
                return Stream.Null;
            return stream;
        }
        

        public override string ToString()
        {
            if (stream == null || disposed)
                return $"{this.GetType().Name}[size=0]";
            else
                return $"{this.GetType().Name}[size={stream.Length}]";
        }

        protected void CheckStreamIsNotNull()
        {
            if (stream == null)
                throw new InvalidOperationException("Operation not permitted. Inner stream is null");
        }

        protected void CheckDisposed()
        {
#if DEBUG
            if (disposed)
                throw new ObjectDisposedException(nameof(BaseRawMessage), "Was disposed at: " + disposeStack);
#endif
            if (disposed)
                throw new ObjectDisposedException(nameof(BaseRawMessage));
        }

        public virtual void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            stream?.Dispose();
            writer?.Dispose();
            reader?.Dispose();
            
#if DEBUG
            disposeStack = Environment.StackTrace;
            if (string.IsNullOrEmpty(disposeStack))
                throw new InvalidOperationException("Stack is null");
#endif
        }
        
                
        #region Read
        
        
        public float ReadSingle()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadSingle();
        }

        public double ReadDouble()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadDouble();
        }

        public bool ReadBoolean()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadBoolean();
        }

        public byte ReadByte()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadByte();
        }

        public short ReadInt16()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadInt16();
        }

        public int ReadInt32()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadInt32();
        }

        public long ReadInt64()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadInt64();
        }

        public sbyte ReadSByte()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadSByte();
        }

        public string ReadString()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadString();
        }

        public ushort ReadUInt16()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadUInt16();
        }

        public uint ReadUInt32()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadUInt32();
        }

        public ulong ReadUInt64()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadUInt64();
        }

        public byte[] ReadBytes(int count)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadBytes(count);
        }

        public int Read(byte[] array, int index, int count)
        {
            CheckDisposed();
            if (stream == null)
                return 0;
            return stream.Read(array, index, count);
        }
        
        public void CopyTo(BaseRawMessage message)
        {
            this.CopyTo(message.stream);
        }
        
        public void CopyTo(BaseRawMessage message, int bytesToCopy)
        {
            this.CopyTo(message.stream, bytesToCopy);
        }

        public void CopyTo(Stream destination)
        {
            CheckDisposed();
            if (stream == null)
                return;
            byte[] buffer = memoryManager.RentArray(memoryManager.DefaultBufferSize);
            try
            {
                int read;
                while ((read = Read(buffer, 0, buffer.Length)) != 0)
                    destination.Write(buffer, 0, read);
            }
            finally
            {
                memoryManager.ReturnArray(buffer);
            }
        }
        
        public void CopyTo(Stream destination, int bytesToCopy)
        {
            CheckDisposed();
            if (stream == null)
                return;
            if (bytesToCopy < 0)
                throw new ArgumentOutOfRangeException(nameof(bytesToCopy));
            if (bytesToCopy == 0)
                return;
            byte[] buffer = memoryManager.RentArray(Math.Min(memoryManager.DefaultBufferSize, bytesToCopy));
            try
            {
                int totalRead = 0;
                int read;
                while (totalRead < bytesToCopy &&
                       (read = Read(buffer, 0, Math.Min(buffer.Length, bytesToCopy - totalRead))) != 0)
                {
                    if (totalRead + read > bytesToCopy)
                        read = bytesToCopy - totalRead;
                    destination.Write(buffer, 0, read);
                    totalRead += read;
                }
            }
            finally
            {
                memoryManager.ReturnArray(buffer);
            }
        }

        public decimal ReadDecimal()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadDecimal();
        }

        public int ReadVarInt32()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return VarintBitConverter.ToInt32(this);
        }

        public long ReadVarInt64()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return VarintBitConverter.ToInt64(this);
        }

        public uint ReadVarUInt32()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return VarintBitConverter.ToUInt32(this);
        }

        public ulong ReadVarUInt64()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return VarintBitConverter.ToUInt64(this);
        }
        
        #endregion
        
        #region Write
        
        public void Write(float value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        public void Write(double value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        public int Write(string value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            int pos = (int)this.writer.BaseStream.Position;
            this.writer.Write(value);
            return (int)this.writer.BaseStream.Position - pos;
        }

        public void Write(bool value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        public void Write(byte value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        public void Write(sbyte value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        public void Write(short value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        public void Write(ushort value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        public void Write(int value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        public void Write(uint value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        public void Write(long value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        public void Write(ulong value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        public void Write(byte[] value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        public void Write(byte[] value, int index, int count)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.stream.Write(value, index, count);
        }

        public void Write(decimal value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }
        
        public void WriteVarInt(int value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            VarintBitConverter.WriteVarintBytes(this, value);
        }

        public void WriteVarInt(uint value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            VarintBitConverter.WriteVarintBytes(this, value);
        }

        public void WriteVarInt(long value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            VarintBitConverter.WriteVarintBytes(this, value);
        }

        public void WriteVarInt(ulong value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            VarintBitConverter.WriteVarintBytes(this, value);
        }
        #endregion
    }
}