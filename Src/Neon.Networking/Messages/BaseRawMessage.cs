using System;
using System.Buffers;
using System.IO;
using System.Text;
using Google.Protobuf;
using Microsoft.IO;
using Neon.Networking.IO;
using Neon.Util.Io;
using Neon.Util.Pooling;

namespace Neon.Networking.Messages
{
    public abstract class BaseRawMessage : IByteReader, IByteWriter, IDisposable
    {
        public static readonly Encoding DEFAULT_ENCODING = Encoding.UTF8;
        protected readonly Encoding _encoding;
        protected readonly IMemoryManager _memoryManager;
        protected readonly BinaryReader _reader;

        protected readonly RecyclableMemoryStream _stream;
        protected readonly BinaryWriter _writer;

        protected bool _disposed;

#if DEBUG
        string _disposeStack;
#endif
        protected bool _readOnly;

        public BaseRawMessage(IMemoryManager memoryManager)
            : this(memoryManager, 0, DEFAULT_ENCODING, Guid.Empty)
        {
        }

        public BaseRawMessage(IMemoryManager memoryManager, int length)
            : this(memoryManager, length, DEFAULT_ENCODING, Guid.Empty)
        {
        }

        public BaseRawMessage(IMemoryManager memoryManager, int length, Encoding encoding)
            : this(memoryManager, length, encoding, Guid.Empty)
        {
        }

        public BaseRawMessage(IMemoryManager memoryManager, int length, Encoding encoding, Guid guid,
            bool readOnly = false)
        {
            Guid = guid;
            _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
            _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            _stream = memoryManager.GetStream(length, guid);
            _reader = new BinaryReader(_stream, encoding, true);
            _writer = new BinaryWriter(_stream, encoding, true);
            _readOnly = readOnly;
        }

        public BaseRawMessage(IMemoryManager memoryManager, ArraySegment<byte> arraySegment, Encoding encoding,
            Guid guid, bool readOnly = false)
        {
            Guid = guid;
            _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
            _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            _stream = memoryManager.GetStream(arraySegment, guid);
            _reader = new BinaryReader(_stream, encoding, true);
            _writer = new BinaryWriter(_stream, encoding, true);
            _readOnly = readOnly;
        }

        /// <summary>
        ///     Unique message identifier
        /// </summary>
        public Guid Guid { get; protected set; }

        /// <summary>
        ///     Get/set the current position in the message
        /// </summary>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        public int Position
        {
            get
            {
                CheckDisposed();
                return (int) _stream.Position;
            }
            set
            {
                CheckDisposed();
                _stream.Position = value;
            }
        }

        /// <summary>
        ///     Get/set the length of the message
        /// </summary>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        public int Length
        {
            get
            {
                CheckDisposed();
                if (_stream == null)
                    return 0;
                return (int) _stream.Length;
            }
            set
            {
                CheckDisposed();
                CheckWrite();
                _stream.SetLength(value);
            }
        }

        /// <summary>
        ///     Returns a sequence containing the contents of the stream.
        /// </summary>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <remarks>IMPORTANT: Calling Dispose() after calling GetReadOnlySequence() invalidates the sequence.</remarks>
        /// <returns>A ReadOnlySequence of bytes</returns>
        public ReadOnlySequence<byte> GetReadOnlySequence()
        {
            CheckDisposed();
            return _stream.GetReadOnlySequence();
        }

        /// <summary>Releases all resources used by the message.</summary>
        public virtual void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _stream?.Dispose();
            _writer?.Dispose();
            _reader?.Dispose();

#if DEBUG
            _disposeStack = Environment.StackTrace;
            if (string.IsNullOrEmpty(_disposeStack))
                throw new InvalidOperationException("Stack is null");
#endif
        }

        internal void MakeReadOnly()
        {
            _readOnly = true;
        }

        public void Advance(int count)
        {
            _stream.Advance(count);
        }

        public Memory<byte> GetMemory(int sizeHint)
        {
            return _stream.GetMemory(sizeHint);
        }

        public Span<byte> GetSpan(int sizeHint)
        {
            return _stream.GetSpan(sizeHint);
        }

        void CheckWrite()
        {
            if (_readOnly)
                throw new InvalidOperationException("Message is read-only");
        }

        public override string ToString()
        {
            if (_stream == null || _disposed)
                return $"{GetType().Name}[size=0]";
            return $"{GetType().Name}[size={_stream.Length}]";
        }

        protected void CheckDisposed()
        {
#if DEBUG
            if (_disposed)
                throw new ObjectDisposedException(nameof(BaseRawMessage), "Was disposed at: " + _disposeStack);
#else
            if (_disposed)
                throw new ObjectDisposedException(nameof(BaseRawMessage));
#endif
        }


        #region Read

        /// <summary>
        ///     Read a delimited protobuf message from the raw message and advances the position by the number of bytes read
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <returns>Protobuf message</returns>
        public T ReadObjectDelimited<T>() where T : IMessage<T>, new()
        {
            CheckDisposed();
            var message = new T();
            message.MergeDelimitedFrom(_stream);
            return message;
        }

        /// <summary>
        ///     Read a non-delimited protobuf message from the raw message and advances the position by the number of bytes read,
        ///     but not more than length
        /// </summary>
        /// <param name="length">Length of the message</param>
        /// <typeparam name="T">Message type</typeparam>
        /// <returns>Protobuf message</returns>
        public T ReadObject<T>(int length) where T : IMessage<T>, new()
        {
            CheckDisposed();
            var message = new T();
            using (var limitedReadOnlyStream =
                   new LimitedReadOnlyStream(_stream, Position, length))
            {
                using (IRentedArray array = _memoryManager.RentArray(_memoryManager.DefaultBufferSize))
                {
                    using (var cis = new CodedInputStream(limitedReadOnlyStream, array.Array, true))
                    {
                        message.MergeFrom(cis);
                    }
                }
            }

            return message;
        }

        /// <summary>
        ///     Read a non-delimited protobuf message from the raw message and advances the position by the number of bytes read
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <returns>Protobuf message</returns>
        public T ReadObject<T>() where T : IMessage<T>, new()
        {
            CheckDisposed();
            var message = new T();
            using (IRentedArray array = _memoryManager.RentArray(_memoryManager.DefaultBufferSize))
            {
                using (var cis = new CodedInputStream(_stream, array.Array, true))
                {
                    message.MergeFrom(cis);
                }
            }

            return message;
        }

        /// <summary>
        ///     Read single value from the message and advances the position by 4 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>single</returns>
        public float ReadSingle()
        {
            CheckDisposed();
            return _reader.ReadSingle();
        }

        /// <summary>
        ///     Read double value from the message and advances the position by 8 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>double</returns>
        public double ReadDouble()
        {
            CheckDisposed();
            return _reader.ReadDouble();
        }

        /// <summary>
        ///     Read boolean value from the message and advances the position by 1 byte
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>bool</returns>
        public bool ReadBoolean()
        {
            CheckDisposed();
            return _reader.ReadBoolean();
        }

        /// <summary>
        ///     Read byte value from the message and advances the position by 1 byte
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>byte</returns>
        public byte ReadByte()
        {
            CheckDisposed();
            return _reader.ReadByte();
        }

        /// <summary>
        ///     Read short value from the message and advances the position by 2 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>short</returns>
        public short ReadInt16()
        {
            CheckDisposed();
            return _reader.ReadInt16();
        }

        /// <summary>
        ///     Read int value from the message and advances the position by 4 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>int</returns>
        public int ReadInt32()
        {
            CheckDisposed();
            return _reader.ReadInt32();
        }

        /// <summary>
        ///     Read long value from the message and advances the position by 8 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>long</returns>
        public long ReadInt64()
        {
            CheckDisposed();
            return _reader.ReadInt64();
        }

        /// <summary>
        ///     Read sbyte value from the message and advances the position by 1 byte
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>sbyte</returns>
        public sbyte ReadSByte()
        {
            CheckDisposed();
            return _reader.ReadSByte();
        }

        /// <summary>
        ///     Reads a string from the current message. The string is prefixed with the length, encoded as an integer seven bits
        ///     at a time
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>String</returns>
        public string ReadString()
        {
            CheckDisposed();
            return _reader.ReadString();
        }

        /// <summary>
        ///     Read ushort value from the message and advances the position by 2 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>ushort</returns>
        public ushort ReadUInt16()
        {
            CheckDisposed();
            return _reader.ReadUInt16();
        }

        /// <summary>
        ///     Read uint value from the message and advances the position by 4 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>uint</returns>
        public uint ReadUInt32()
        {
            CheckDisposed();
            return _reader.ReadUInt32();
        }

        /// <summary>
        ///     Read ulong value from the message and advances the position by 8 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>ulong</returns>
        public ulong ReadUInt64()
        {
            CheckDisposed();
            return _reader.ReadUInt64();
        }

        /// <summary>
        ///     Read a byte array from the message
        /// </summary>
        /// <param name="count">
        ///     The number of bytes to read. This value must be 0 or a non-negative number or an exception will
        ///     occur.
        /// </param>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="count" /> is negative.</exception>
        /// <returns>
        ///     A byte array containing data read from the message. This might be less than the number of bytes requested if
        ///     the end of the message is reached.
        /// </returns>
        public byte[] ReadBytes(int count)
        {
            CheckDisposed();
            return _reader.ReadBytes(count);
        }

        /// <summary>
        ///     Reads from the current position into the provided buffer.
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
            if (_stream == null)
                return 0;
            return _stream.Read(array, index, count);
        }

        /// <summary>
        ///     Copies the contents of this message into destination message.
        /// </summary>
        /// <param name="message">The message to copy items into.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        public void CopyTo(BaseRawMessage message)
        {
            CopyTo(message._stream);
        }

        /// <summary>
        ///     Copies the contents of this message into destination message.
        /// </summary>
        /// <param name="message">The message to copy items into.</param>
        /// <param name="bytesToCopy">Amount of bytes to copy</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        public void CopyTo(BaseRawMessage message, int bytesToCopy)
        {
            CopyTo(message._stream, bytesToCopy);
        }

        /// <summary>
        ///     Copies the contents of this message into the stream.
        /// </summary>
        /// <param name="destination">The stream to copy items into.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        public void CopyTo(Stream destination)
        {
            CheckDisposed();
            if (_stream == null)
                return;
            using (IRentedArray array = _memoryManager.RentArray(_memoryManager.DefaultBufferSize))
            {
                int read;
                while ((read = Read(array.Array, 0, array.Array.Length)) != 0)
                    destination.Write(array.Array, 0, read);
            }
        }

        /// <summary>
        ///     Copies the contents of this message into the stream.
        /// </summary>
        /// <param name="destination">The stream to copy items into.</param>
        /// <param name="bytesToCopy">The amount of bytes to copy.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        public void CopyTo(Stream destination, int bytesToCopy)
        {
            CheckDisposed();
            if (_stream == null)
                return;
            if (bytesToCopy < 0)
                throw new ArgumentOutOfRangeException(nameof(bytesToCopy));
            if (bytesToCopy == 0)
                return;
            using (IRentedArray array = _memoryManager.RentArray(Math.Min(_memoryManager.DefaultBufferSize, bytesToCopy)))
            {
                var totalRead = 0;
                int read;
                while (totalRead < bytesToCopy &&
                       (read = Read(array.Array, 0, Math.Min(array.Array.Length, bytesToCopy - totalRead))) != 0)
                {
                    if (totalRead + read > bytesToCopy)
                        read = bytesToCopy - totalRead;
                    destination.Write(array.Array, 0, read);
                    totalRead += read;
                }
            }
        }

        /// <summary>
        ///     Read decimal value from the message and advances the position by 16 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>decimal</returns>
        public decimal ReadDecimal()
        {
            CheckDisposed();
            return _reader.ReadDecimal();
        }

        /// <summary>
        ///     Read variable int value from the message and advances the position by 1-5 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>int</returns>
        public int ReadVarInt32()
        {
            CheckDisposed();
            return VarintBitConverter.ToInt32(this);
        }

        /// <summary>
        ///     Read variable long value from the message and advances the position by 1-9 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>long</returns>
        public long ReadVarInt64()
        {
            CheckDisposed();
            return VarintBitConverter.ToInt64(this);
        }

        /// <summary>
        ///     Read variable uint value from the message and advances the position by 1-5 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>uint</returns>
        public uint ReadVarUInt32()
        {
            CheckDisposed();
            return VarintBitConverter.ToUInt32(this);
        }

        /// <summary>
        ///     Read variable ulong value from the message and advances the position by 1-9 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>ulong</returns>
        public ulong ReadVarUInt64()
        {
            CheckDisposed();
            return VarintBitConverter.ToUInt64(this);
        }

        #endregion

        #region Write

        /// <summary>
        ///     Writes a float value to the current message and advances the position by 4 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(float value)
        {
            CheckDisposed();
            CheckWrite();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes a double value to the current message and advances the position by 8 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(double value)
        {
            CheckDisposed();
            CheckWrite();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes a length-prefixed string to this message in the current encoding of the message, and advances the current
        ///     position of the message in accordance with the encoding used and the specific characters being written to the
        ///     stream.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>Amount of bytes written</returns>
        public int Write(string value)
        {
            CheckDisposed();
            CheckWrite();
            var pos = (int) _writer.BaseStream.Position;
            _writer.Write(value);
            return (int) _writer.BaseStream.Position - pos;
        }

        /// <summary>
        ///     Writes a boolean value to the current message and advances the position by 1 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(bool value)
        {
            CheckDisposed();
            CheckWrite();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes a bytea value to the current message and advances the position by 1 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(byte value)
        {
            CheckDisposed();
            CheckWrite();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes a sbyte value to the current message and advances the position by 1 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(sbyte value)
        {
            CheckDisposed();
            CheckWrite();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes a short value to the current message and advances the position by 2 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(short value)
        {
            CheckDisposed();
            CheckWrite();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes a ushort value to the current message and advances the position by 2 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(ushort value)
        {
            CheckDisposed();
            CheckWrite();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes an int value to the current message and advances the position by 4 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(int value)
        {
            CheckDisposed();
            CheckWrite();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes a uint value to the current message and advances the position by 4 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(uint value)
        {
            CheckDisposed();
            CheckWrite();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes a long value to the current message and advances the position by 8 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(long value)
        {
            CheckDisposed();
            CheckWrite();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes a ulong value to the current message and advances the position by 8 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(ulong value)
        {
            CheckDisposed();
            CheckWrite();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes a byte array to the underlying message and advances the position by the length of the array
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
        public void Write(byte[] value)
        {
            CheckDisposed();
            CheckWrite();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes a buffer to the underlying message
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
            CheckWrite();
            _stream.Write(value, index, count);
        }

        /// <summary>
        ///     Writes a decimal value to the current message and advances the position by 16 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(decimal value)
        {
            CheckDisposed();
            CheckWrite();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes variable int value to the message, and advances the position by the 1-5 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void WriteVarInt(int value)
        {
            CheckDisposed();
            CheckWrite();
            VarintBitConverter.WriteVarintBytes(this, value);
        }

        /// <summary>
        ///     Writes variable uint value to the message, and advances the position by the 1-5 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void WriteVarInt(uint value)
        {
            CheckDisposed();
            CheckWrite();
            VarintBitConverter.WriteVarintBytes(this, value);
        }

        /// <summary>
        ///     Writes variable long value to the message, and advances the position by the 1-9 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void WriteVarInt(long value)
        {
            CheckDisposed();
            CheckWrite();
            VarintBitConverter.WriteVarintBytes(this, value);
        }

        /// <summary>
        ///     Writes variable ulong value to the message, and advances the position by the 1-9 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void WriteVarInt(ulong value)
        {
            CheckDisposed();
            CheckWrite();
            VarintBitConverter.WriteVarintBytes(this, value);
        }

        /// <summary>
        ///     Write a delimited protobuf message to the raw message and advances the position
        /// </summary>
        /// <param name="message">Protobuf message</param>
        public void WriteObjectDelimited(IMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            CheckDisposed();
            CheckWrite();
            using (IRentedArray array = _memoryManager.RentArray(_memoryManager.DefaultBufferSize))
            {
                using (var cos = new CodedOutputStream(_stream, array.Array, true))
                {
                    cos.WriteLength(message.CalculateSize());
                    message.WriteTo(cos);
                }
            }
        }

        /// <summary>
        ///     Write a protobuf message to the raw message and advances the position
        /// </summary>
        /// <param name="message">Protobuf message</param>
        public void WriteObject(IMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            CheckDisposed();
            CheckWrite();
            using (IRentedArray array = _memoryManager.RentArray(_memoryManager.DefaultBufferSize))
            {
                using (var cos = new CodedOutputStream(_stream, array.Array, true))
                {
                    message.WriteTo(cos);
                }
            }
        }

        #endregion
    }
}