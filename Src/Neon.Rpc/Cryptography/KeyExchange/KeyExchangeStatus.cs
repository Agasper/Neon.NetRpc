namespace Neon.Rpc.Cryptography.KeyExchange
{
    enum KeyExchangeStatus : byte
    {
        Initial,
        ClientKeyDataGenerated,
        CommonKeySet
    }
}