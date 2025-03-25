using System;
using System.Threading.Tasks;
using Neon.Networking.Messages;

namespace Neon.Networking.Cryptography.Ciphers
{
    interface IRpcCipher : ICipher, IDisposable
    {
        int KeySize { get; }
        Task KeySetTask { get; }

        void SetKey(byte[] key);
    }
}