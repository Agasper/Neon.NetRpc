using Neon.Rpc.Net;

namespace Neon.Rpc.Controllers
{
    class RpcControllerContext
    {
        public RpcConfiguration Configuration { get; }
        public IRpcConnectionInternal Connection { get; }
        
        public RpcControllerContext(RpcConfiguration configuration, IRpcConnectionInternal rpcConnection)
        {
            Configuration = configuration;
            Connection = rpcConnection;
        }
    }
}
