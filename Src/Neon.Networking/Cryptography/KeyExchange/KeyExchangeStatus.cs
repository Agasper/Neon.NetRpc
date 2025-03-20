namespace Neon.Networking.Cryptography.KeyExchange
{
    enum KeyExchangeStatus : byte
    {
        Initial,
        ClientKeyDataGenerated,
        CommonKeySet
    }
}