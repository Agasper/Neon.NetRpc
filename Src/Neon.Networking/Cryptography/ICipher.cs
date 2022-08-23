using System;
using System.Security.Cryptography;

namespace Neon.Networking.Cryptography
{
    public interface ICipher : IDisposable
    {
        int KeySize { get; }

        void SetKey(byte[] key);
        
        ICryptoTransform CreateEncryptor();
        ICryptoTransform CreateDecryptor();
    }
}
