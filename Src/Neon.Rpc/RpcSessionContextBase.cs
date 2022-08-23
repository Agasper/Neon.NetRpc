using Neon.Logging;

namespace Neon.Rpc
{
    public class RpcSessionContextBase
    {
        internal IRpcConnectionInternal ConnectionInternal { get;  }
        
        public ILogManager LogManager { get;  }
        public IRpcConnection Connection => ConnectionInternal;

        internal RpcSessionContextBase(ILogManager logManager, IRpcConnectionInternal rpcConnectionInternal)
        {
            this.LogManager = logManager;
            this.ConnectionInternal = rpcConnectionInternal;
        }
    }
}
