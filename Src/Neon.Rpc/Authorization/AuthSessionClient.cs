using System;
using System.Threading;
using System.Threading.Tasks;
using Neon.Rpc.Payload;

namespace Neon.Rpc.Authorization
{
    /// <summary>
    /// RpcClient auth session
    /// </summary>
    public class AuthSessionClient : AuthSessionBase
    {
        /// <summary>
        /// Result of a remote authentication operation
        /// </summary>
        public object AuthResult { get; private set; }

        TaskCompletionSource<object> tcs;

        public AuthSessionClient(AuthSessionContext sessionContext) : base(sessionContext)
        {
            
        }

        internal async Task<object> Start(object arg, CancellationToken cancellationToken)
        {
            if (this.tcs != null)
                throw new InvalidOperationException($"{nameof(Start)} could be called only once");
            
            this.tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            
            AuthenticationRequest request = new AuthenticationRequest();
            request.Argument = arg;
            request.HasArgument = true;
            cancellationToken.ThrowIfCancellationRequested();
            await SendNeonMessage(request);
            logger.Debug($"{LogsSign} auth request sent!");

            using (cancellationToken.Register(() =>
                   {
                       tcs.TrySetException(new OperationCanceledException("Authentication cancelled"));
                   }))
            {
                return await tcs.Task.ConfigureAwait(false);
            }
        }

        private protected override void AuthenticationResponse(AuthenticationResponse authenticationResponse)
        {
            logger.Debug($"{LogsSign} auth response received {authenticationResponse}");
            if (authenticationResponse.Exception == null)
            {
                logger.Debug($"{LogsSign} auth done!");
                if (authenticationResponse.HasArgument)
                    this.AuthResult = authenticationResponse.Argument;
                else
                    this.AuthResult = null;
                tcs.TrySetResult(authenticationResponse.Argument);
            }
            else
            {
                logger.Debug($"#{connection.Id} auth failed: {authenticationResponse.Exception.Message}!");
                tcs.TrySetException(authenticationResponse.Exception);
            }
        }
    }
}