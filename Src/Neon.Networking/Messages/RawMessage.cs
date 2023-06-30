using System;
using System.IO.Compression;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using Neon.Util.Pooling;

namespace Neon.Networking.Messages
{
    public class RawMessage : BaseRawMessage, IRawMessage
    {
        /// <summary>Gets a value indicating whether the current message is compressed.</summary>
        /// <returns>
        ///     <see langword="true" /> if the message is compressed; otherwise, <see langword="false" />.
        /// </returns>
        public bool Compressed { get; }

        /// <summary>Gets a value indicating whether the current message is encrypted.</summary>
        /// <returns>
        ///     <see langword="true" /> if the message is encrypted; otherwise, <see langword="false" />.
        /// </returns>
        public bool Encrypted { get; }

        /// <summary>
        ///     Compressing the message, returning a new compressed one
        /// </summary>
        /// <param name="compressionLevel">Compression level</param>
        /// <returns>A compressed message</returns>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">If the message not compressed</exception>
        public RawMessage Compress(CompressionLevel compressionLevel)
        {
            CheckDisposed();
            if (Compressed)
                throw new InvalidOperationException($"{nameof(RawMessage)} already compressed");
            var newGuid = Guid.NewGuid();
            var compressedMessage = new RawMessage(_memoryManager, Length, true, Encrypted, _encoding, newGuid);
            if (_stream == null || _stream.Length == 0)
                return compressedMessage;
            _stream.Position = 0;

            using (var gzip = new GZipOutputStream(compressedMessage._stream)) //TODO add array pool
            {
                gzip.SetLevel(6);
                gzip.IsStreamOwner = false;
                CopyTo(gzip);
            }

            compressedMessage._stream.Position = 0;
            return compressedMessage;
        }
        
        public override string ToString()
        {
            if (_stream == null || _disposed)
                return $"{GetType().Name}[size=0]";
            return $"{GetType().Name}[size={_stream.Length},compressed={Compressed},encrypted={Encrypted}]";
        }

        /// <summary>
        ///     Decompressing the message, returning a new decompressed one
        /// </summary>
        /// <returns>A decompressed message</returns>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">If the message not compressed</exception>
        public RawMessage Decompress()
        {
            CheckDisposed();
            if (!Compressed)
                throw new InvalidOperationException($"{nameof(RawMessage)} isn't compressed");
            var newGuid = Guid.NewGuid();
            var decompressedMessage = new RawMessage(_memoryManager, Length, false, Encrypted, _encoding, newGuid);
            if (_stream == null || _stream.Length == 0)
                return decompressedMessage;
            _stream.Position = 0;

            using (IRentedArray array = _memoryManager.RentArray(_memoryManager.DefaultBufferSize))
            {
                using (var gzip = new GZipInputStream(_stream)) //TODO add array pool
                {
                    gzip.IsStreamOwner = false;

                    int read;
                    while ((read = gzip.Read(array.Array, 0, array.Array.Length)) != 0)
                        decompressedMessage.Write(array.Array, 0, read);
                }
            }

            decompressedMessage._stream.Position = 0;

            return decompressedMessage;
        }

        /// <summary>
        ///     Encrypting the message, returning a new encrypted one
        /// </summary>
        /// <param name="cipher">An instance of the cipher, used for encryption</param>
        /// <returns>An encrypted message</returns>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">If the message not compressed</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="cipher" /> is <see langword="null" />.</exception>
        public RawMessage Encrypt(ICipher cipher)
        {
            CheckDisposed();
            if (cipher == null) throw new ArgumentNullException(nameof(cipher));
            if (Encrypted)
                throw new InvalidOperationException($"{nameof(RawMessage)} already encrypted");
            var newGuid = Guid.NewGuid();
            var encryptedMessage = new RawMessage(_memoryManager, Length, Compressed, true, _encoding, newGuid);
            if (_stream == null || _stream.Length == 0)
                return encryptedMessage;
        
            _stream.Position = 0;
        
            using (IRentedArray array = _memoryManager.RentArray(_memoryManager.DefaultBufferSize))
                cipher.Encrypt(_stream, encryptedMessage._stream, array.Array);
        
            encryptedMessage._stream.Position = 0;
        
            return encryptedMessage;
        }
        
        /// <summary>
        ///     Decrypting the message, returning a new decrypted one
        /// </summary>
        /// <param name="cipher">An instance of the cipher, used for decryption</param>
        /// <returns>A decrypted message</returns>
        /// <exception cref="T:System.ObjectDisposedException">The message is disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">If the message not compressed</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="cipher" /> is <see langword="null" />.</exception>
        public RawMessage Decrypt(ICipher cipher)
        {
            CheckDisposed();
            if (cipher == null) throw new ArgumentNullException(nameof(cipher));
            if (!Encrypted)
                throw new InvalidOperationException($"{nameof(RawMessage)} isn't encrypted");
            var newGuid = Guid.NewGuid();
            var decryptedMessage = new RawMessage(_memoryManager, Length, Compressed, false, _encoding, newGuid);
            if (_stream == null || _stream.Length == 0)
                return decryptedMessage;
            _stream.Position = 0;
        
            using (IRentedArray array = _memoryManager.RentArray(_memoryManager.DefaultBufferSize))
                cipher.Decrypt(_stream, decryptedMessage._stream, array.Array);
        
            decryptedMessage._stream.Position = 0;
        
            return decryptedMessage;
        }

        #region Constructors

        internal RawMessage(IMemoryManager memoryManager)
            : this(memoryManager, 0, false, false, DEFAULT_ENCODING, Guid.NewGuid())
        {
        }

        internal RawMessage(IMemoryManager memoryManager, int length)
            : this(memoryManager, length, false, false, DEFAULT_ENCODING, Guid.NewGuid())
        {
        }

        internal RawMessage(IMemoryManager memoryManager, int length,
            bool compressed, bool encrypted)
            : this(memoryManager, length, compressed, encrypted, DEFAULT_ENCODING, Guid.NewGuid())
        {
        }

        internal RawMessage(IMemoryManager memoryManager, int length,
            bool compressed, bool encrypted, Guid guid, bool readOnly = false)
            : this(memoryManager, length, compressed, encrypted, DEFAULT_ENCODING, guid, readOnly)
        {
        }

        internal RawMessage(IMemoryManager memoryManager, int length,
            bool compressed, bool encrypted, Encoding encoding, Guid guid, bool readOnly = false)
            : base(memoryManager, length, encoding, guid, readOnly)
        {
            Compressed = compressed;
            Encrypted = encrypted;
        }

        internal RawMessage(IMemoryManager memoryManager, ArraySegment<byte> arraySegment, bool readOnly = false)
            : this(memoryManager, arraySegment, false, false, DEFAULT_ENCODING, Guid.NewGuid(), readOnly)
        {
        }

        internal RawMessage(IMemoryManager memoryManager, ArraySegment<byte> arraySegment,
            bool compressed, bool encrypted, Encoding encoding, Guid guid, bool readOnly = false)
            : base(memoryManager, arraySegment, encoding, guid, readOnly)
        {
            Compressed = compressed;
            Encrypted = encrypted;
        }

        #endregion
    }
}