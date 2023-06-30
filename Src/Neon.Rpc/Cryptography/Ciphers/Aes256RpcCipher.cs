using Neon.Rpc.Cryptography.Ciphers;

namespace Neon.Networking.Cryptography
{
    public class Aes256RpcCipher : AesBaseRpcCipher
    {
        public Aes256RpcCipher()
        {
            _cipher.KeySize = 256;
        }
    }
}