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
        
        /// <summary>
        /// Unique message identifier
        /// </summary>
        public Guid Guid { get; protected set; }
        
        /// <summary>
        /// Get/set the current position in the message
        /// </summary>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
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

        /// <summary>
        /// Get/set the length of the message
        /// </summary>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
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

        /// <summary>
        /// Returns a sequence containing the contents of the stream.
        /// </summary>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <remarks>IMPORTANT: Calling Dispose() after calling GetReadOnlySequence() invalidates the sequence.</remarks>
        /// <returns>A ReadOnlySequence of bytes</returns>
        public ReadOnlySequence<byte> GetReadOnlySequence()
        {
            CheckDisposed();
            if (stream == null)
                return new ReadOnlySequence<byte>();
            return stream.GetReadOnlySequence();
        }

        /// <summary>
        /// Returns a message as Stream
        /// </summary>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <remarks>IMPORTANT: Calling Dispose() after calling AsStream() invalidates the stream.</remarks>
        /// <returns>Stream</returns>
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

        /// <summary>Releases all resources used by the message.</summary>
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
        
        /// <summary>
        /// Read single value from the message and advances the position by 4 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>single</returns>
        public float ReadSingle()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadSingle();
        }

        /// <summary>
        /// Read double value from the message and advances the position by 8 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>double</returns>
        public double ReadDouble()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadDouble();
        }

        /// <summary>
        /// Read boolean value from the message and advances the position by 1 byte
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>bool</returns>
        public bool ReadBoolean()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadBoolean();
        }

        /// <summary>
        /// Read byte value from the message and advances the position by 1 byte
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>byte</returns>
        public byte ReadByte()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadByte();
        }

        /// <summary>
        /// Read short value from the message and advances the position by 2 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>short</returns>
        public short ReadInt16()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadInt16();
        }

        /// <summary>
        /// Read int value from the message and advances the position by 4 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>int</returns>
        public int ReadInt32()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadInt32();
        }

        /// <summary>
        /// Read long value from the message and advances the position by 8 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>long</returns>
        public long ReadInt64()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadInt64();
        }

        /// <summary>
        /// Read sbyte value from the message and advances the position by 1 byte
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>sbyte</returns>
        public sbyte ReadSByte()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadSByte();
        }

        /// <summary>
        /// Reads a string from the current message. The string is prefixed with the length, encoded as an integer seven bits at a time
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>String</returns>
        public string ReadString()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadString();
        }

        /// <summary>
        /// Read ushort value from the message and advances the position by 2 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>ushort</returns>
        public ushort ReadUInt16()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadUInt16();
        }
        
        /// <summary>
        /// Read uint value from the message and advances the position by 4 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>uint</returns>
        public uint ReadUInt32()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadUInt32();
        }

        /// <summary>
        /// Read ulong value from the message and advances the position by 8 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>ulong</returns>
        public ulong ReadUInt64()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadUInt64();
        }

        /// <summary>
        /// Read a byte array from the message
        /// </summary>
        /// <param name="count">The number of bytes to read. This value must be 0 or a non-negative number or an exception will occur.</param>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="count" /> is negative.</exception>
        /// <returns>A byte array containing data read from the message. This might be less than the number of bytes requested if the end of the message is reached.</returns>
        public byte[] ReadBytes(int count)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadBytes(count);
        }

        /// <summary>
        /// Reads from the current position into the provided buffer.
        /// </summary>
        /// <param name="array">Destination buffer.</param>
        /// <param name="index">Offset into buffer at which to start placing the read bytes.</param>
        /// <param name="count">Number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
        /// <exception cref="ArgumentNullException">buffer is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">offset or count is less than 0.</exception>
        /// <exception cref="ArgumentException">offset subtracted from the buffer length is less than count.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        public int Read(byte[] array, int index, int count)
        {
            CheckDisposed();
            if (stream == null)
                return 0;
            return stream.Read(array, index, count);
        }
        
        /// <summary>
        /// Copies the contents of this message into destination message. 
        /// </summary>
        /// <param name="message">The message to copy items into.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        public void CopyTo(BaseRawMessage message)
        {
            this.CopyTo(message.stream);
        }
        
        /// <summary>
        /// Copies the contents of this message into destination message. 
        /// </summary>
        /// <param name="message">The message to copy items into.</param>
        /// <param name="bytesToCopy">Amount of bytes to copy</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        public void CopyTo(BaseRawMessage message, int bytesToCopy)
        {
            
            this.CopyTo(message.stream, bytesToCopy);
        }

        /// <summary>
        /// Copies the contents of this message into the stream. 
        /// </summary>
        /// <param name="destination">The stream to copy items into.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
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
        
        /// <summary>
        /// Copies the contents of this message into the stream. 
        /// </summary>
        /// <param name="destination">The stream to copy items into.</param>
        /// <param name="bytesToCopy">The amount of bytes to copy.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
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

        /// <summary>
        /// Read decimal value from the message and advances the position by 16 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>decimal</returns>
        public decimal ReadDecimal()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return reader.ReadDecimal();
        }

        /// <summary>
        /// Read variable int value from the message and advances the position by 1-5 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>int</returns>
        public int ReadVarInt32()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return VarintBitConverter.ToInt32(this);
        }

        /// <summary>
        /// Read variable long value from the message and advances the position by 1-9 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>long</returns>
        public long ReadVarInt64()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return VarintBitConverter.ToInt64(this);
        }

        /// <summary>
        /// Read variable uint value from the message and advances the position by 1-5 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>uint</returns>
        public uint ReadVarUInt32()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return VarintBitConverter.ToUInt32(this);
        }

        /// <summary>
        /// Read variable ulong value from the message and advances the position by 1-9 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>ulong</returns>
        public ulong ReadVarUInt64()
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            return VarintBitConverter.ToUInt64(this);
        }
        
        #endregion
        
        #region Write
        
        /// <summary>
        /// Writes a float value to the current message and advances the position by 4 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(float value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a double value to the current message and advances the position by 8 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(double value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a length-prefixed string to this message in the current encoding of the message, and advances the current position of the message in accordance with the encoding used and the specific characters being written to the stream.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>Amount of bytes written</returns>
        public int Write(string value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            int pos = (int)this.writer.BaseStream.Position;
            this.writer.Write(value);
            return (int)this.writer.BaseStream.Position - pos;
        }

        /// <summary>
        /// Writes a boolean value to the current message and advances the position by 1 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(bool value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a bytea value to the current message and advances the position by 1 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(byte value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a sbyte value to the current message and advances the position by 1 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(sbyte value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a short value to the current message and advances the position by 2 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(short value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a ushort value to the current message and advances the position by 2 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(ushort value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes an int value to the current message and advances the position by 4 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(int value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a uint value to the current message and advances the position by 4 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(uint value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a long value to the current message and advances the position by 8 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(long value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a ulong value to the current message and advances the position by 8 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(ulong value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a byte array to the underlying message and advances the position by the length of the array
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
        public void Write(byte[] value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes a buffer to the underlying message
        /// </summary>
        /// <param name="value">Source buffer.</param>
        /// <param name="index">Start position.</param>
        /// <param name="count">Number of bytes to write.</param>
        /// <exception cref="ArgumentNullException">buffer is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">offset or count is negative.</exception>
        /// <exception cref="ArgumentException">buffer.Length - offset is not less than count.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(byte[] value, int index, int count)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.stream.Write(value, index, count);
        }

        /// <summary>
        /// Writes a decimal value to the current message and advances the position by 16 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(decimal value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            this.writer.Write(value);
        }
        
        /// <summary>
        /// Writes variable int value to the message, and advances the position by the 1-5 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void WriteVarInt(int value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            VarintBitConverter.WriteVarintBytes(this, value);
        }

        /// <summary>
        /// Writes variable uint value to the message, and advances the position by the 1-5 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void WriteVarInt(uint value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            VarintBitConverter.WriteVarintBytes(this, value);
        }

        /// <summary>
        /// Writes variable long value to the message, and advances the position by the 1-9 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void WriteVarInt(long value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            VarintBitConverter.WriteVarintBytes(this, value);
        }

        /// <summary>
        /// Writes variable ulong value to the message, and advances the position by the 1-9 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void WriteVarInt(ulong value)
        {
            CheckDisposed();
            CheckStreamIsNotNull();
            VarintBitConverter.WriteVarintBytes(this, value);
        }
        #endregion
    }
}