using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Neon.Networking;
using Neon.Networking.Messages;
using Neon.Networking.Udp.Messages;

namespace Neon.Rpc
{
    interface IRpcConnectionInternal : IRpcConnection
    {
        RawMessage CreateMessage();
        RawMessage CreateMessage(int length);
        Task SendMessage(IRawMessage message, DeliveryType deliveryType, int channel, CancellationToken cancellationToken);
    }
    
    /// <summary>
    /// Representation of the transport connection
    /// </summary>
    public interface IRpcConnection
    {
        /// <summary>
        /// User session
        /// </summary>
        RpcSessionBase UserSession { get; }
        /// <summary>
        /// Cancellation token
        /// </summary>
        CancellationToken CancellationToken { get; }
        /// <summary>
        /// Unique connection identifier
        /// </summary>
        long Id { get; }
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

        /// <summary>
        /// Closes the underlying transport connection
        /// </summary>
        void Close();
    }
}
