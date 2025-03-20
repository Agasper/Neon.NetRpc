using System;
using Neon.Networking.Messages;

namespace Neon.Networking.Cryptography.Ciphers
{
    interface IRpcCipher : ICipher, IDisposable
    {
        int KeySize { get; }

        void SetKey(byte[] key);
    }
}