using System.Threading;

namespace Neon.Rpc
{
    public struct ExecutionOptions
    {
        /// <summary>
        /// User-defined state
        /// </summary>
        public object State { get; set; }
        /// <summary>
        /// Cancellation token for the operation
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        public ExecutionOptions(object state, CancellationToken cancellationToken)
        {
            State = state;
            CancellationToken = cancellationToken;
        }

        public ExecutionOptions WithState(object state)
        {
            State = state;
            return this;
        }

        public ExecutionOptions WithCancellationToken(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            return this;
        }

        public override string ToString()
        {
            return $"{nameof(ExecutionOptions)}[state={State}]";
        }

        public static ExecutionOptions Default => new ExecutionOptions(null, CancellationToken.None);
    }
}
