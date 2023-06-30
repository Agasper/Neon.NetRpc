using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Neon.Logging;
using Neon.Rpc.Controllers;

namespace Neon.Rpc
{
    public class RpcSessionContext
    {
        public Any AuthenticationResult { get; }
        public object AuthenticationState { get; }
        public IRpcConnection Connection { get; }
        public ILogManager LogManager { get; }
        internal RpcController Controller { get; }
        internal TaskScheduler TaskScheduler { get; }
        
        internal RpcSessionContext(RpcController controller, object authenticationState, IRpcConnection connection,
            Any authenticationResult, TaskScheduler taskScheduler, ILogManager logManager)
        {
            LogManager = logManager;
            Controller = controller;
            Connection = connection;
            AuthenticationState = authenticationState;
            AuthenticationResult = authenticationResult;
            TaskScheduler = taskScheduler;
        }
    }
}