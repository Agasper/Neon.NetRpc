using System.Threading;

namespace Neon.Rpc
{
    public struct ExecutionOptions
    {
        /// <summary>
        /// Timeout for the request
        /// </summary>
        public int Timeout { get; set; }
        /// <summary>
        /// User-defined object state
        /// </summary>
        public object State { get; set; }
        /// <summary>
        /// Cancellation token for the operation
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        public ExecutionOptions(int timeout, object state, CancellationToken cancellationToken)
        {
            this.Timeout = timeout;
            this.State = state;
            this.CancellationToken = cancellationToken;
        }

        public ExecutionOptions WithTimeout(int timeout)
        {
            this.Timeout = timeout;
            return this;
        }
        
        public ExecutionOptions WithCancellationToken(CancellationToken cancellationToken)
        {
            this.CancellationToken = cancellationToken;
            return this;
        }

        public override string ToString()
        {
            return $"{nameof(ExecutionOptions)}[state={State},timeout={Timeout}]";
        }

        public static ExecutionOptions Default => new ExecutionOptions(System.Threading.Timeout.Infinite, null, CancellationToken.None);
    }
}
