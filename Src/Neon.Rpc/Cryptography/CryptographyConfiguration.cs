namespace Neon.Rpc.Cryptography
{
    public struct CryptographyConfiguration
    {
        public EncryptionAlgorithmEnum EncryptionAlgorithm { get; set; }
        public KeyExchangeAlgorithmEnum KeyExchangeAlgorithm { get; set; }
    }
}