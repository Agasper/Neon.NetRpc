namespace Neon.Networking.Cryptography.Ciphers
{
    public class Aes256RpcCipher : AesBaseRpcCipher
    {
        public Aes256RpcCipher()
        {
            _cipher.KeySize = 256;
        }
    }
}