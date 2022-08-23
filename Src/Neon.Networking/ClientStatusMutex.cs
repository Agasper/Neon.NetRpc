using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neon.Networking
{
    public class ClientStatusMutex : IDisposable
    {
        readonly SemaphoreSlim semaphoreSlim;
        
        public ClientStatusMutex()
        {
            this.semaphoreSlim = new SemaphoreSlim(1, 1);
        }

        public Task WaitForStatusChange(CancellationToken cancellationToken)
        {
            return this.semaphoreSlim.WaitAsync(cancellationToken);
        }
        
        public Task WaitForStatusChange()
        {
            return this.semaphoreSlim.WaitAsync();
        }

        public void EndStatusChange()
        {
            this.semaphoreSlim.Release();
        }

        public void Dispose()
        {
            semaphoreSlim?.Dispose();
        }
    }
}