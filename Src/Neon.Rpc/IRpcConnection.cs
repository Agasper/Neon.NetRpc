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
    
    /// <summary>
    /// Representation of the transport connection
    /// </summary>
    public interface IRpcConnection
    {
        /// <summary>
        /// Unique connection identifier
        /// </summary>
        long Id { get; }
        /// <summary>
        /// User-defined tag
        /// </summary>
        object Tag { get; }
        /// <summary>
        /// Connection statistics
        /// </summary>
        IConnectionStatistics Statistics { get; }
        /// <summary>
        /// Does connection belongs to the client
        /// </summary>
        bool IsClientConnection { get; }
        /// <summary>
        /// Is connection active
        /// </summary>
        bool Connected { get; }
        /// <summary>
        /// Connection endpoint
        /// </summary>
        EndPoint RemoteEndpoint { get; }
    }
}
