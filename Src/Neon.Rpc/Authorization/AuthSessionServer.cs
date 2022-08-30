using System;
using System.Threading;
using System.Threading.Tasks;
using Neon.Rpc.Payload;

namespace Neon.Rpc.Authorization
{
    /// <summary>
    /// A non-async version of server authentication session
    /// </summary>
    public abstract class AuthSessionServer : AuthSessionServerBase
    {
        protected AuthSessionServer(AuthSessionContext sessionContext)
            : base(sessionContext)
        {

        }
        
        private protected override void AuthenticationRequest(AuthenticationRequest authenticationRequest)
        {
            if (!authenticationRequest.HasArgument)
                throw new InvalidOperationException("Auth request has no argument");

            logger.Debug($"{LogsSign} auth request received!");

            object argument = null;
            if (authenticationRequest.HasArgument)
                argument = authenticationRequest.Argument;

            Task.Factory.StartNew(Auth, argument, CancellationToken.None, TaskCreationOptions.None,
                sessionContext.TaskScheduler).ContinueWith(AuthFinishedAsync, TaskContinuationOptions.ExecuteSynchronously);
        }

        protected abstract object Auth(object arg);
    }
}