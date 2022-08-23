using System;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using Microsoft.IO;
using Neon.Networking.Cryptography;
using Neon.Networking.IO;
using Neon.Util.Pooling;

namespace Neon.Networking.Messages
{
    public class RawMessage : BaseRawMessage
    {
        public bool Compressed { get; }
        public bool Encrypted { get; }

        #region Constructors

        internal RawMessage(IMemoryManager memoryManager, RecyclableMemoryStream stream,
            bool compressed, bool encrypted)
            : this(memoryManager, stream, compressed, encrypted, DEFAULT_ENCODING, System.Guid.Empty)
        {

        }
        
        internal RawMessage(IMemoryManager memoryManager, RecyclableMemoryStream stream,
            bool compressed, bool encrypted, Guid guid)
            : this(memoryManager, stream, compressed, encrypted, DEFAULT_ENCODING, guid)
        {

        }

        internal RawMessage(IMemoryManager memoryManager, RecyclableMemoryStream stream,
            bool compressed, bool encrypted, Encoding encoding, Guid guid)
                : base(memoryManager, stream, encoding, guid)
        {
            this.Compressed = compressed;
            this.Encrypted = encrypted;
        }

        #endregion
        

        public RawMessage Compress(CompressionLevel compressionLevel)
        {
            CheckDisposed();
            if (this.Compressed)
                throw new InvalidOperationException($"{nameof(RawMessage)} already compressed");
            Guid newGuid = Guid.NewGuid();
            var compressedMessage = new RawMessage(memoryManager, memoryManager.GetStream(this.Length, newGuid), true, this.Encrypted, this.encoding, newGuid);
            if (this.stream == null || this.stream.Length == 0)
                return compressedMessage;
            this.stream.Position = 0;

            using (GZipOutputStream gzip = new GZipOutputStream(compressedMessage.stream)) //TODO add array pool
            {
                gzip.SetLevel(6);
                gzip.IsStreamOwner = false;
                this.CopyTo(gzip);
            }

            compressedMessage.stream.Position = 0;
            return compressedMessage;
        }
        
        public RawMessage Decompress()
        {
            CheckDisposed();
            if (!this.Compressed)
                throw new InvalidOperationException($"{nameof(RawMessage)} isn't compressed");
            Guid newGuid = Guid.NewGuid();
            var decompressedMessage = new RawMessage(memoryManager, memoryManager.GetStream(newGuid), false, this.Encrypted, this.encoding, newGuid);
            if (this.stream == null || this.stream.Length == 0)
                return decompressedMessage;
            this.stream.Position = 0;

            byte[] buffer = memoryManager.RentArray(memoryManager.DefaultBufferSize);
            try
            {
                using (GZipInputStream gzip = new GZipInputStream(this.stream)) //TODO add array pool
                {
                    gzip.IsStreamOwner = false;

                    int read;
                    while ((read = gzip.Read(buffer, 0, buffer.Length)) != 0)
                        decompressedMessage.Write(buffer, 0, read);
                }
            }
            finally
            {
                memoryManager.ReturnArray(buffer);
            }

            decompressedMessage.stream.Position = 0;

            return decompressedMessage;
        }

        public RawMessage Encrypt(ICipher cipher)
        {
            CheckDisposed();
            if (cipher == null) throw new ArgumentNullException(nameof(cipher));
            if (this.Encrypted)
                throw new InvalidOperationException($"{nameof(RawMessage)} already encrypted");
            Guid newGuid = Guid.NewGuid();
            var encryptedMessage = new RawMessage(memoryManager, memoryManager.GetStream(this.Length, newGuid), this.Compressed, true, this.encoding, newGuid);
            if (this.stream == null || this.stream.Length == 0)
                return encryptedMessage;
            
            this.stream.Position = 0;
            using (var encryptor = cipher.CreateEncryptor())
            {
                using (CryptoStream cryptoStream = new CryptoStream(new NonDisposableStream(encryptedMessage.stream),
                           encryptor, CryptoStreamMode.Write))
                {
                    this.CopyTo(cryptoStream);
                    cryptoStream.FlushFinalBlock();
                }
            }

            encryptedMessage.stream.Position = 0;

            return encryptedMessage;
        }

        
        public RawMessage Decrypt(ICipher cipher)
        {
            CheckDisposed(); 
            CheckStreamIsNotNull();
            if (cipher == null) throw new ArgumentNullException(nameof(cipher));
            if (!this.Encrypted)
                throw new InvalidOperationException($"{nameof(RawMessage)} isn't encrypted");
            Guid newGuid = Guid.NewGuid();
            var decryptedMessage = new RawMessage(memoryManager, memoryManager.GetStream(this.Length, newGuid), this.Compressed, false, this.encoding, newGuid);
            if (this.stream == null || this.stream.Length == 0)
                return decryptedMessage;
            this.stream.Position = 0;
            
            byte[] buffer = memoryManager.RentArray(memoryManager.DefaultBufferSize);
            try
            {

                using (var encryptor = cipher.CreateDecryptor())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(new NonDisposableStream(this.stream), encryptor,
                               CryptoStreamMode.Read))
                    {
                        int read;
                        while ((read = cryptoStream.Read(buffer, 0, buffer.Length)) != 0)
                            decryptedMessage.Write(buffer, 0, read);
                    }
                }
            }
            finally
            {
                memoryManager.ReturnArray(buffer);
            }

            decryptedMessage.stream.Position = 0;

            return decryptedMessage;
        }


    }
}
