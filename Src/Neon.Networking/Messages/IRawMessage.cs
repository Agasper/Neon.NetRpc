using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using Google.Protobuf;

namespace Neon.Networking.Messages
{
    public interface IRawMessage : IBufferWriter<byte>, IDisposable
    {
        /// <summary>
        ///     Unique message identifier
        /// </summary>
        Guid Guid { get; }

        /// <summary>
        ///     Get/set the current position in the message
        /// </summary>
        int Position { get; set; }

        /// <summary>
        ///     Get/set the length of the message
        /// </summary>
        int Length { get; set; }
        
        /// <summary>Gets a value indicating whether the current message is compressed.</summary>
        /// <returns>
        ///     <see langword="true" /> if the message is compressed; otherwise, <see langword="false" />.
        /// </returns>
        bool Compressed { get; }

        /// <summary>Gets a value indicating whether the current message is encrypted.</summary>
        /// <returns>
        ///     <see langword="true" /> if the message is encrypted; otherwise, <see langword="false" />.
        /// </returns>
        bool Encrypted { get; }


        /// <summary>
        ///     Copies the contents of this message into destination message.
        /// </summary>
        /// <param name="message">The message to copy items into.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        void CopyTo(BaseRawMessage message);

        /// <summary>
        ///     Copies the contents of this message into destination message.
        /// </summary>
        /// <param name="message">The message to copy items into.</param>
        /// <param name="bytesToCopy">Amount of bytes to copy</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        void CopyTo(BaseRawMessage message, int bytesToCopy);
        
        /// <summary>
        ///     Copies the contents of this message into the stream.
        /// </summary>
        /// <param name="destination">The stream to copy items into.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        void CopyTo(Stream destination);
        
        /// <summary>
        ///     Copies the contents of this message into the stream.
        /// </summary>
        /// <param name="destination">The stream to copy items into.</param>
        /// <param name="bytesToCopy">The amount of bytes to copy.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        void CopyTo(Stream destination, int bytesToCopy);

        /// <summary>
        ///     Decrypting the message, returning a new decrypted one
        /// </summary>
        /// <param name="cipher">An instance of the cipher, used for decryption</param>
        /// <returns>A decrypted message</returns>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">If the message not compressed</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="cipher" /> is <see langword="null" />.</exception>
        RawMessage Decrypt(ICipher cipher);

        /// <summary>
        ///     Encrypting the message, returning a new encrypted one
        /// </summary>
        /// <param name="cipher">An instance of the cipher, used for encryption</param>
        /// <returns>An encrypted message</returns>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">If the message not compressed</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="cipher" /> is <see langword="null" />.</exception>
        RawMessage Encrypt(ICipher cipher);
        
        /// <summary>
        ///     Compressing the message, returning a new compressed one
        /// </summary>
        /// <param name="compressionLevel">Compression level</param>
        /// <returns>A compressed message</returns>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">If the message not compressed</exception>
        RawMessage Compress(CompressionLevel compressionLevel);

        /// <summary>
        ///     Decompressing the message, returning a new decompressed one
        /// </summary>
        /// <returns>A decompressed message</returns>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">If the message not compressed</exception>
        RawMessage Decompress();

        /// <summary>
        ///     Returns a sequence containing the contents of the stream.
        /// </summary>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <remarks>IMPORTANT: Calling Dispose() after calling GetReadOnlySequence() invalidates the sequence.</remarks>
        /// <returns>A ReadOnlySequence of bytes</returns>
        ReadOnlySequence<byte> GetReadOnlySequence();

        /// <summary>
        ///     Read a delimited protobuf message from the raw message and advances the position by the number of bytes read
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <returns>Protobuf message</returns>
        T ReadObjectDelimited<T>() where T : IMessage<T>, new();

        /// <summary>
        ///     Read a non-delimited protobuf message from the raw message and advances the position by the number of bytes read,
        ///     but not more than length
        /// </summary>
        /// <param name="length">Length of the message</param>
        /// <typeparam name="T">Message type</typeparam>
        /// <returns>Protobuf message</returns>
        T ReadObject<T>(int length) where T : IMessage<T>, new();

        /// <summary>
        ///     Read a non-delimited protobuf message from the raw message and advances the position by the number of bytes read
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <returns>Protobuf message</returns>
        T ReadObject<T>() where T : IMessage<T>, new();

        /// <summary>
        ///     Reads a string from the current message. The string is prefixed with the length, encoded as an integer seven bits
        ///     at a time
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>String</returns>
        string ReadString();

        /// <summary>
        ///     Read boolean value from the message and advances the position by 1 byte
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>bool</returns>
        bool ReadBoolean();

        /// <summary>
        ///     Read single value from the message and advances the position by 4 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>single</returns>
        float ReadSingle();

        /// <summary>
        ///     Read double value from the message and advances the position by 8 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>double</returns>
        double ReadDouble();

        /// <summary>
        ///     Read byte value from the message and advances the position by 1 byte
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>byte</returns>
        byte ReadByte();

        /// <summary>
        ///     Read sbyte value from the message and advances the position by 1 byte
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>sbyte</returns>
        sbyte ReadSByte();

        /// <summary>
        ///     Read short value from the message and advances the position by 2 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>short</returns>
        short ReadInt16();

        /// <summary>
        ///     Read ushort value from the message and advances the position by 2 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>ushort</returns>
        ushort ReadUInt16();

        /// <summary>
        ///     Read int value from the message and advances the position by 4 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>int</returns>
        int ReadInt32();

        /// <summary>
        ///     Read uint value from the message and advances the position by 4 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>uint</returns>
        uint ReadUInt32();

        /// <summary>
        ///     Read long value from the message and advances the position by 8 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>long</returns>
        long ReadInt64();

        /// <summary>
        ///     Read ulong value from the message and advances the position by 8 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>ulong</returns>
        ulong ReadUInt64();

        /// <summary>
        ///     Read decimal value from the message and advances the position by 16 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>decimal</returns>
        decimal ReadDecimal();

        /// <summary>
        ///     Read variable int value from the message and advances the position by 1-5 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>int</returns>
        int ReadVarInt32();

        /// <summary>
        ///     Read variable uint value from the message and advances the position by 1-5 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>uint</returns>
        uint ReadVarUInt32();

        /// <summary>
        ///     Read variable long value from the message and advances the position by 1-9 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>long</returns>
        long ReadVarInt64();

        /// <summary>
        ///     Read variable ulong value from the message and advances the position by 1-9 bytes
        /// </summary>
        /// <exception cref="T:System.IO.EndOfStreamException">The end of the message is reached.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>ulong</returns>
        ulong ReadVarUInt64();

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
        byte[] ReadBytes(int count);

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
        int Read(byte[] array, int index, int count);

        /// <summary>
        ///     Writes a length-prefixed string to this message in the current encoding of the message, and advances the current
        ///     position of the message in accordance with the encoding used and the specific characters being written to the
        ///     stream.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <returns>Amount of bytes written</returns>
        int Write(string value);

        /// <summary>
        ///     Writes a float value to the current message and advances the position by 4 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        void Write(float value);

        /// <summary>
        ///     Writes a double value to the current message and advances the position by 8 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        void Write(double value);

        /// <summary>
        ///     Writes a boolean value to the current message and advances the position by 1 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        void Write(bool value);

        /// <summary>
        ///     Writes a bytea value to the current message and advances the position by 1 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        void Write(byte value);

        /// <summary>
        ///     Writes a sbyte value to the current message and advances the position by 1 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        void Write(sbyte value);

        /// <summary>
        ///     Writes a short value to the current message and advances the position by 2 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        void Write(short value);

        /// <summary>
        ///     Writes a ushort value to the current message and advances the position by 2 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        void Write(ushort value);

        /// <summary>
        ///     Writes an int value to the current message and advances the position by 4 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        void Write(int value);

        /// <summary>
        ///     Writes a uint value to the current message and advances the position by 4 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        void Write(uint value);

        /// <summary>
        ///     Writes a long value to the current message and advances the position by 8 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        void Write(long value);

        /// <summary>
        ///     Writes a ulong value to the current message and advances the position by 8 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        void Write(ulong value);

        /// <summary>
        ///     Writes a decimal value to the current message and advances the position by 16 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        void Write(decimal value);

        /// <summary>
        ///     Writes a byte array to the underlying message and advances the position by the length of the array
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
        void Write(byte[] value);

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
        void Write(byte[] value, int index, int count);

        /// <summary>
        ///     Writes variable int value to the message, and advances the position by the 1-5 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        void WriteVarInt(int value);

        /// <summary>
        ///     Writes variable uint value to the message, and advances the position by the 1-5 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        void WriteVarInt(uint value);

        /// <summary>
        ///     Writes variable long value to the message, and advances the position by the 1-9 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        void WriteVarInt(long value);

        /// <summary>
        ///     Writes variable ulong value to the message, and advances the position by the 1-9 bytes
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The message is empty</exception>
        void WriteVarInt(ulong value);

        /// <summary>
        ///     Write a delimited protobuf message to the raw message and advances the position
        /// </summary>
        /// <param name="message"></param>
        void WriteObjectDelimited(IMessage message);

        /// <summary>
        ///     Write a protobuf message to the raw message and advances the position
        /// </summary>
        /// <param name="message">Protobuf message</param>
        void WriteObject(IMessage message);
    }
}