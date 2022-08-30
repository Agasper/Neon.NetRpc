using System;
using System.Threading.Tasks;
using Neon.Rpc.Payload;
using Neon.Util;

namespace Neon.Rpc.Authorization
{
    /// <summary>
    /// An async version of server authentication session
    /// </summary>
    public abstract class AuthSessionServerAsync : AuthSessionServerBase
    {
        protected AuthSessionServerAsync(AuthSessionContext sessionContext)
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
            
            taskFactory.StartNew((s) => { return Auth(s); }, argument)
                .Unwrap()
                .ContinueWith(AuthFinishedAsync, TaskContinuationOptions.ExecuteSynchronously);
        }

        protected abstract Task<object> Auth(object arg);
    }
}