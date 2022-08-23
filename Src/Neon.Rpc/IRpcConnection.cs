using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Neon.Networking;
using Neon.Networking.Messages;
using Neon.Networking.Udp.Messages;
using Neon.Rpc.Serialization;

namespace Neon.Rpc
{
    interface IRpcConnectionInternal : IRpcConnection
    {
        RpcMessage CreateRpcMessage();
        RpcMessage CreateRpcMessage(int length);
        
        Task SendMessage(RpcMessage message, DeliveryType deliveryType, int channel);
        void Close();
    }
    
    public interface IRpcConnection
    {
        long Id { get; }
        object Tag { get; }
        IConnectionStatistics Statistics { get; }
        bool IsClientConnection { get; }
        bool Connected { get; }
        EndPoint RemoteEndpoint { get; }
    }
}
