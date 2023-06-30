using System;
using Neon.Networking.Cryptography;
using Neon.Rpc.Cryptography.Ciphers;
using Neon.Rpc.Cryptography.KeyExchange;
using Neon.Util.Pooling;

namespace Neon.Rpc.Cryptography
{
    class CryptographyHelper
    {
        public static IRpcCipher CreateCipher(EncryptionAlgorithmEnum encryptionAlgorithm)
        {
            switch (encryptionAlgorithm)
            {
                case EncryptionAlgorithmEnum.Aes128:
                    return new Aes128RpcCipher();
                case EncryptionAlgorithmEnum.Aes256:
                    return new Aes256RpcCipher();
                default:
                    throw new NotSupportedException(
                        $"Encryption algorithm {encryptionAlgorithm} is not supported");
            }
        }

        public static IKeyExchangeAlgorithm CreateKeyExchangeAlgorithm(IMemoryManager memoryManager, KeyExchangeAlgorithmEnum keyExchangeAlgorithm, IRpcCipher rpcCipher)
        {
            switch (keyExchangeAlgorithm)
            {
                case KeyExchangeAlgorithmEnum.Rsa2048:
                    return new RsaKeyExchange(memoryManager,2048, rpcCipher.KeySize);
                case KeyExchangeAlgorithmEnum.Rsa1024:
                    return new RsaKeyExchange(memoryManager,1024, rpcCipher.KeySize);
                default:
                    throw new NotSupportedException(
                        $"Encryption algorithm {keyExchangeAlgorithm} is not supported");
            }
        }
    }
}