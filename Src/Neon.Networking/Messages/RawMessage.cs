using System;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using Neon.Util.Pooling;

namespace Neon.Networking.Messages
{
    public class RawMessage : BaseRawMessage, IRawMessage
    {
        internal MessageFlagsEnum WrapMessage(int compressionThreshold, int compressionLevel, ICipher cipher,
            out RawMessage finalMessage)
        {
            finalMessage = this;
            MessageFlagsEnum flags = MessageFlagsEnum.None;
            
            if (finalMessage.Length > compressionThreshold)
            {
                flags |= MessageFlagsEnum.Compressed;
                finalMessage = finalMessage.Compress(compressionLevel);
            }

            if (cipher != null)
            {
                flags |= MessageFlagsEnum.Encrypted;
                var oldMessage = finalMessage;
                finalMessage = finalMessage.Encrypt(cipher);
                if (oldMessage.Guid != this.Guid)
                    oldMessage.Dispose();
            }

            return flags;
        }
        
        internal MessageFlagsEnum UnrapMessage(MessageFlagsEnum flags, ICipher cipher, out RawMessage finalMessage)
        {
            if (flags.HasFlag(MessageFlagsEnum.Encrypted) && cipher == null)
                throw new ArgumentException("Cipher not provided, but encrypted flag is set");
            
            var newFlags = flags;
            finalMessage = this;
            
            if (newFlags.HasFlag(MessageFlagsEnum.Encrypted))
            {
                newFlags &= ~MessageFlagsEnum.Encrypted;
                finalMessage = finalMessage.Decrypt(cipher);
            }
            
            if (newFlags.HasFlag(MessageFlagsEnum.Compressed))
            {
                newFlags &= ~MessageFlagsEnum.Compressed;
                var oldMessage = finalMessage;
                finalMessage = finalMessage.Decompress();
                if (oldMessage.Guid != this.Guid)
                    oldMessage.Dispose();
            }

            return newFlags;
        }


        internal RawMessage Compress(int level)
        {
            CheckDisposed();
            var newGuid = Guid.NewGuid();
            var compressedMessage = new RawMessage(_memoryManager, Length, _encoding, newGuid);
            if (_stream == null || _stream.Length == 0)
                return compressedMessage;
            _stream.Position = 0;

            using (var gzip = new GZipOutputStream(compressedMessage._stream)) //TODO add array pool
            {
                gzip.SetLevel(level);
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
            return $"{GetType().Name}[size={_stream.Length},guid={Guid}]";
        }

        internal RawMessage Decompress()
        {
            CheckDisposed();
            var newGuid = Guid.NewGuid();
            var decompressedMessage = new RawMessage(_memoryManager, Length, _encoding, newGuid);
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


        internal RawMessage Encrypt(ICipher cipher)
        {
            CheckDisposed();
            if (cipher == null) throw new ArgumentNullException(nameof(cipher));
            var newGuid = Guid.NewGuid();
            var encryptedMessage = new RawMessage(_memoryManager, Length, _encoding, newGuid);
            if (_stream == null || _stream.Length == 0)
                return encryptedMessage;
        
            _stream.Position = 0;
        
            using (IRentedArray array = _memoryManager.RentArray(_memoryManager.DefaultBufferSize))
                cipher.Encrypt(_stream, encryptedMessage._stream, array.Array);
        
            encryptedMessage._stream.Position = 0;
        
            return encryptedMessage;
        }
        
        internal RawMessage Decrypt(ICipher cipher)
        {
            CheckDisposed();
            if (cipher == null) throw new ArgumentNullException(nameof(cipher));
            var newGuid = Guid.NewGuid();
            var decryptedMessage = new RawMessage(_memoryManager, Length, _encoding, newGuid);
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
            : this(memoryManager, 0, DEFAULT_ENCODING, Guid.NewGuid())
        {
        }

        internal RawMessage(IMemoryManager memoryManager, int length)
            : this(memoryManager, length, DEFAULT_ENCODING, Guid.NewGuid())
        {
        }

        internal RawMessage(IMemoryManager memoryManager, int length,
            Guid guid, bool readOnly = false)
            : this(memoryManager, length, DEFAULT_ENCODING, guid, readOnly)
        {
        }

        internal RawMessage(IMemoryManager memoryManager, int length,
            Encoding encoding, Guid guid, bool readOnly = false)
            : base(memoryManager, length, encoding, guid, readOnly)
        {

        }

        internal RawMessage(IMemoryManager memoryManager, ArraySegment<byte> arraySegment, bool readOnly = false)
            : this(memoryManager, arraySegment, DEFAULT_ENCODING, Guid.NewGuid(), readOnly)
        {
        }

        internal RawMessage(IMemoryManager memoryManager, ArraySegment<byte> arraySegment,
            Encoding encoding, Guid guid, bool readOnly = false)
            : base(memoryManager, arraySegment, encoding, guid, readOnly)
        {
        }

        #endregion
    }
}