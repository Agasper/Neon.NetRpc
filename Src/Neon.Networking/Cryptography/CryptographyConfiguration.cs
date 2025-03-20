namespace Neon.Networking.Cryptography
{
    public struct CryptographyConfiguration
    {
        public EncryptionAlgorithmEnum EncryptionAlgorithm { get; }
        public KeyExchangeAlgorithmEnum KeyExchangeAlgorithm { get; }

        public CryptographyConfiguration(EncryptionAlgorithmEnum encryptionAlgorithm, KeyExchangeAlgorithmEnum keyExchangeAlgorithm)
        {
            EncryptionAlgorithm = encryptionAlgorithm;
            KeyExchangeAlgorithm = keyExchangeAlgorithm;
        }
    }
}