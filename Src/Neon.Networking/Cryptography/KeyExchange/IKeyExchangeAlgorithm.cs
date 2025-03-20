using System;
using System.Threading.Tasks;

namespace Neon.Networking.Cryptography.KeyExchange
{
    interface IKeyExchangeAlgorithm : IDisposable
    {
        KeyExchangeStatus Status { get; }
        int KeySize { get; }
        Task KeyExchangeCompleted { get; }
        
        ArraySegment<byte> GenerateClientKeyData();
        ArraySegment<byte> KeyDataExchange(ArraySegment<byte> keyData);
        void UpdateServerKeyData(ArraySegment<byte> keyData);
        
        ArraySegment<byte> GetKey();
    }
}