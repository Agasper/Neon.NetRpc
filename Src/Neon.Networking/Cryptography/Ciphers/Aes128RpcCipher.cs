namespace Neon.Networking.Cryptography.Ciphers
{
    public class Aes128RpcCipher : AesBaseRpcCipher
    {
        public Aes128RpcCipher()
        {
            _cipher.KeySize = 128;
        }
    }
}