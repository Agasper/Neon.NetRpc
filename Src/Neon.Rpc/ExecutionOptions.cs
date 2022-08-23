using System.Threading;

namespace Neon.Rpc
{
    public struct ExecutionOptions
    {
        public int Timeout { get; set; }
        public object State { get; set; }
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
