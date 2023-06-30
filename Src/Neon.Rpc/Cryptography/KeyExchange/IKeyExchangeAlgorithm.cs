using Google.Protobuf;

namespace Neon.Rpc.Cryptography.KeyExchange
{
    interface IKeyExchangeAlgorithm
    {
        KeyExchangeStatus Status { get; }
        int KeySize { get; }
        
        ByteString GenerateClientKeyData();
        ByteString KeyDataExchange(ByteString keyData);
        void UpdateServerKeyData(ByteString keyData);
        
        byte[] GetKey();
    }
}