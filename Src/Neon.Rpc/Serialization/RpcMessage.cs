using System;
using System.Buffers;
using System.IO;
using Neon.Networking.Messages;

namespace Neon.Rpc.Serialization
{
    public interface IRpcMessage : IRawMessage
    {
        /// <summary>
        /// Reading an object from the message
        /// </summary>
        /// <returns>A new instance of a serialized object</returns>
        object ReadObject();
        /// <summary>
        /// Serializes an object to the message
        /// </summary>
        /// <param name="obj">Object to serialize</param>
        void WriteObject(object obj);
    }
    
    public class RpcMessage : IRpcMessage
    {
        /// <summary>
        /// Unique message identifier
        /// </summary>
        public Guid Guid => message.Guid;
        
        /// <summary>
        /// Get/set the length of the message
        /// </summary>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        public int Length
        {
            get => message.Length;
            set => message.Length = value;
        }
        
        /// <summary>
        /// Get/set the current position in the message
        /// </summary>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
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

        /// <summary>Releases all resources used by the message.</summary>
        public void Dispose()
        {
            this.message.Dispose();
        }

        /// <summary>
        /// Reading an object from the message
        /// </summary>
        /// <returns>A new instance of a serialized object</returns>
        public object ReadObject()
        {
            return this.serializer.ParseBinary(this);
        }

        /// <summary>
        /// Serializes an object to the message
        /// </summary>
        /// <param name="obj">Object to serialize</param>
        public void WriteObject(object obj)
        {
            this.serializer.WriteBinary(this, obj);
        }

        /// <summary>
        /// Returns a message as Stream
        /// </summary>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <remarks>IMPORTANT: Calling Dispose() after calling AsStream() invalidates the stream.</remarks>
        /// <returns>Stream</returns>
        public Stream AsStream()
        {
            return this.message.AsStream();
        }

        /// <summary>
        /// Returns a sequence containing the contents of the stream.
        /// </summary>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <remarks>IMPORTANT: Calling Dispose() after calling GetReadOnlySequence() invalidates the sequence.</remarks>
        /// <returns>A ReadOnlySequence of bytes</returns>
        public ReadOnlySequence<byte> GetReadOnlySequence()
        {
            return message.GetReadOnlySequence();
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
            return this.message.ReadString();
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
            return this.message.ReadBoolean();
        }

        /// <summary>
        /// Read single value from the message and advances the position by 4 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>single</returns>
        public float ReadSingle()
        {
            return this.message.ReadSingle();
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
            return this.message.ReadDouble();
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
            return this.message.ReadByte();
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
            return this.message.ReadSByte();
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
            return this.message.ReadInt16();
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
            return this.message.ReadUInt16();
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
            return this.message.ReadInt32();
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
            return this.message.ReadUInt32();
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
            return this.message.ReadInt64();
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
            return this.message.ReadUInt64();
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
            return this.message.ReadDecimal();
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
            return this.message.ReadVarInt32();
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
            return this.message.ReadVarUInt32();
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
            return this.message.ReadVarInt64();
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
            return this.message.ReadVarUInt64();
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
            return this.message.ReadBytes(count);
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
            return this.message.Write(value);
        }

        /// <summary>
        /// Writes a float value to the current message and advances the position by 4 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(float value)
        {
            this.message.Write(value);
        }

        /// <summary>
        /// Writes a double value to the current message and advances the position by 8 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(double value)
        {
            this.message.Write(value);
        }

        /// <summary>
        /// Writes a boolean value to the current message and advances the position by 1 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(bool value)
        {
            this.message.Write(value);
        }

        /// <summary>
        /// Writes a bytea value to the current message and advances the position by 1 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(byte value)
        {
            this.message.Write(value);
        }

        /// <summary>
        /// Writes a sbyte value to the current message and advances the position by 1 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(sbyte value)
        {
            this.message.Write(value);
        }

        /// <summary>
        /// Writes a short value to the current message and advances the position by 2 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(short value)
        {
            this.message.Write(value);
        }

        /// <summary>
        /// Writes a ushort value to the current message and advances the position by 2 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(ushort value)
        {
            this.message.Write(value);
        }

        /// <summary>
        /// Writes an int value to the current message and advances the position by 4 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(int value)
        {
            this.message.Write(value);
        }

        /// <summary>
        /// Writes a uint value to the current message and advances the position by 4 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(uint value)
        {
            this.message.Write(value);
        }

        /// <summary>
        /// Writes a long value to the current message and advances the position by 8 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(long value)
        {
            this.message.Write(value);
        }

        /// <summary>
        /// Writes a ulong value to the current message and advances the position by 8 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(ulong value)
        {
            this.message.Write(value);
        }

        /// <summary>
        /// Writes a decimal value to the current message and advances the position by 16 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void Write(decimal value)
        {
            this.message.Write(value);
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
            this.message.Write(value);
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
            this.message.Write(value, index, count);
        }

        /// <summary>
        /// Writes variable int value to the message, and advances the position by the 1-5 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void WriteVarInt(int value)
        {
            this.message.WriteVarInt(value);
        }

        /// <summary>
        /// Writes variable uint value to the message, and advances the position by the 1-5 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void WriteVarInt(uint value)
        {
            this.message.WriteVarInt(value);
        }

        /// <summary>
        /// Writes variable long value to the message, and advances the position by the 1-9 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void WriteVarInt(long value)
        {
            this.message.WriteVarInt(value);
        }

        /// <summary>
        /// Writes variable ulong value to the message, and advances the position by the 1-9 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        public void WriteVarInt(ulong value)
        {
            this.message.WriteVarInt(value);
        }
    }
}