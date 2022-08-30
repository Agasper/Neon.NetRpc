using System;
using System.Threading.Tasks;
using Neon.Rpc.Payload;
using Neon.Util;

namespace Neon.Rpc.Authorization
{
    /// <summary>
    /// A base class for server auth sessions
    /// </summary>
    public abstract class AuthSessionServerBase : AuthSessionBase
    {
        private protected readonly TaskFactory<Task<object>> taskFactory;
        private protected readonly AuthSessionContext sessionContext;
        
        public AuthSessionServerBase(AuthSessionContext sessionContext) : base(sessionContext)
        {
            this.sessionContext = sessionContext;
            this.taskFactory = new TaskFactory<Task<object>>(sessionContext.TaskScheduler);
        }
        

        private protected async Task AuthFinishedAsync(Task<object> t)
        {
            try
            {
                if (t.IsFaulted)
                {
                    var innermost = t.Exception.GetInnermostException();
                    if (innermost is RemotingException aex)
                        await AuthFinishedFailed(aex).ConfigureAwait(false);
                    else
                        await AuthFinishedFailed(new RemotingException(innermost))
                            .ConfigureAwait(false);
                }
                else
                {
                    await AuthFinishedSuccess(t.Result).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"{LogsSign} unhandled exception in auth processing: {ex}");
                connection.Close();
            }
        }
        
        private protected async Task AuthFinishedSuccess(object result)
        {
            AuthenticationResponse response = new AuthenticationResponse();
            response.Exception = null;
            response.Argument = result;
            response.HasArgument = true;
            
            logger.Debug($"{LogsSign} auth done!");
            
            sessionContext.SuccessCallback?.Invoke(this);

            await SendNeonMessage(response).ConfigureAwait(false);
            
            
        }
        
        private protected async Task AuthFinishedFailed(RemotingException ex)
        {
            if (ex == null)
                throw new ArgumentNullException(nameof(ex));
            
            AuthenticationResponse response = new AuthenticationResponse();
            response.Exception = ex;
            response.HasArgument = false;
            response.Argument = null;
            
            logger.Debug($"{LogsSign} auth failed: {ex.Message}!");
            
            await SendNeonMessage(response).ConfigureAwait(false);
        }

    }
}