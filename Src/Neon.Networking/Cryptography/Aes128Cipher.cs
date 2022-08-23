using System;
using System.Security.Cryptography;

namespace Neon.Networking.Cryptography
{
    public class Aes128Cipher : ICipher
    {
        public int KeySize => cipher.KeySize;
        
        Aes cipher;
        bool keyInitialized = false;

        public Aes128Cipher()
        {
            cipher = Aes.Create();
            cipher.KeySize = 128;
            cipher.BlockSize = 128;
            cipher.Padding = PaddingMode.ISO10126;
            cipher.Mode = CipherMode.CBC;
        }

        public void SetKey(byte[] key, byte[] iv)
        {
            if (keyInitialized)
                throw new InvalidOperationException("Key already initialized");
            
            cipher.Key = key;
            cipher.IV = iv;
            keyInitialized = true;
        }
        
        public void SetKey(byte[] key)
        {
            byte[] iv = new byte[cipher.BlockSize / 8];
            Buffer.BlockCopy(key, 0, iv, 0, iv.Length);
            this.SetKey(key, iv);
        }

        public ICryptoTransform CreateEncryptor()
        {
            if (!keyInitialized)
                throw new InvalidOperationException("Key isn't set");
            return cipher.CreateEncryptor();
        }

        public ICryptoTransform CreateDecryptor()
        {
            if (!keyInitialized)
                throw new InvalidOperationException("Key isn't set");
            return cipher.CreateDecryptor();
        }

        public void Dispose()
        {
            cipher.Dispose();
        }
    }
}
