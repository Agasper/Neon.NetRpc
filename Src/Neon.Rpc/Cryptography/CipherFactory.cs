using Neon.Networking.Cryptography;

namespace Neon.Rpc.Cryptography
{
    interface ICipherFactory
    {
        ICipher CreateNewCipher();
    }
    
    class CipherFactory<T> : ICipherFactory where T : ICipher, new()
    {
        public CipherFactory()
        {
        }

        public ICipher CreateNewCipher()
        {
            return new T();
        }
    }
}