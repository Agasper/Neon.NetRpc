using System;
using System.Threading.Tasks;
using Neon.Logging;

namespace Neon.Rpc.Authorization
{
    public class AuthSessionContext : RpcSessionContextBase
    {
        /// <summary>
        /// A task scheduler from configuration
        /// </summary>
        public TaskScheduler TaskScheduler { get; }
        internal DAuthPassedCallback SuccessCallback { get; }

        internal AuthSessionContext(TaskScheduler taskScheduler, ILogManager logManager,
            IRpcConnectionInternal rpcConnectionInternal, DAuthPassedCallback successCallback)
            : base(logManager, rpcConnectionInternal)
        {
            this.TaskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
            this.SuccessCallback = successCallback;
        }
    }
}