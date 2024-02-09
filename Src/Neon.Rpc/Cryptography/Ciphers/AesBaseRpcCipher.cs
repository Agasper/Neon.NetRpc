using System;
using System.IO;
using System.Security.Cryptography;
using Neon.Networking.Cryptography;
using Neon.Networking.IO;
using Neon.Util.Io;

namespace Neon.Rpc.Cryptography.Ciphers
{
    public abstract class AesBaseRpcCipher : IRpcCipher
    {
        public int KeySize => _cipher.KeySize;
        
        protected Aes _cipher;
        protected bool _keyInitialized;

        public AesBaseRpcCipher()
        {
            _cipher = Aes.Create();
            _cipher.BlockSize = 128;
            _cipher.Padding = PaddingMode.ISO10126;
            _cipher.Mode = CipherMode.CBC;
        }

        public void SetKey(byte[] key)
        {
            var iv = new byte[_cipher.BlockSize / 8];
            Buffer.BlockCopy(key, 0, iv, 0, iv.Length);
            SetKey(key, iv);
        }
        
        // public ICryptoTransform CreateEncryptor()
        // {
        //     if (!_keyInitialized)
        //         throw new InvalidOperationException("Key isn't set");
        //     return _cipher.CreateEncryptor();
        // }
        //
        // public ICryptoTransform CreateDecryptor()
        // {
        //     if (!_keyInitialized)
        //         throw new InvalidOperationException("Key isn't set");
        //     return _cipher.CreateDecryptor();
        // }

        public void Dispose()
        {
            _cipher.Dispose();
        }

        public void SetKey(byte[] key, byte[] iv)
        {
            if (_keyInitialized)
                throw new InvalidOperationException("Key already initialized");

            _cipher.Key = key;
            _cipher.IV = iv;
            _keyInitialized = true;
        }

        public void Encrypt(Stream source, Stream destination, byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length == 0) throw new ArgumentNullException(nameof(buffer));
            if (!_keyInitialized)
                throw new InvalidOperationException("Key isn't set");
            
            using (var encryptor = _cipher.CreateEncryptor())
            {
                using (var cryptoStream = new CryptoStream(new NonDisposableStream(destination), encryptor,
                           CryptoStreamMode.Write))
                {
                    int read;
                    while ((read = source.Read(buffer, 0, buffer.Length)) != 0)
                        cryptoStream.Write(buffer, 0, read);
                    cryptoStream.FlushFinalBlock();
                }
            }
        }

        public void Decrypt(Stream source, Stream destination, byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length == 0) throw new ArgumentNullException(nameof(buffer));
            if (!_keyInitialized)
                throw new InvalidOperationException("Key isn't set");

            using (var decryptor = _cipher.CreateDecryptor())
            {
                using (var cryptoStream = new CryptoStream(new NonDisposableStream(source), decryptor,
                           CryptoStreamMode.Read))
                {
                    int read;
                    while ((read = cryptoStream.Read(buffer, 0, buffer.Length)) != 0)
                        destination.Write(buffer, 0, read);
                }
            }
        }
    }
}