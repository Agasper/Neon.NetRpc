using System.Collections.Concurrent;
using Neon.Logging;

namespace Neon.Test.Util;

public class SingleThreadSynchronizationContext : SynchronizationContext
{
    class SynchronizationInfo
    {
        public SendOrPostCallback callback;
        public object? state;
        public ManualResetEvent? ev;
        public Exception? exception;

        public SynchronizationInfo(SendOrPostCallback callback, object? state, ManualResetEvent? ev)
        {
            this.callback = callback;
            this.state = state;
            this.ev = ev;
            exception = null;
        }
    }

    public delegate void DOnContextException(Exception ex);

    public int ThreadId => thread.ManagedThreadId;
    public event DOnContextException? OnException;
    
    readonly ILogger logger;
    readonly ConcurrentQueue<SynchronizationInfo> queue;
    readonly Thread thread;
    
    bool stop;

    public SingleThreadSynchronizationContext()
    {
        queue = new ConcurrentQueue<SynchronizationInfo>();
        thread = new Thread(ThreadRun);
        thread.IsBackground = true;
        logger = LogManager.Default.GetLogger(nameof(SingleThreadSynchronizationContext));
    }
    
    public SingleThreadSynchronizationContext(ILogManager logManager) : this()
    {
        logger = logManager.GetLogger(nameof(SingleThreadSynchronizationContext));
    }

    public void Start()
    {
        stop = false;
        thread.Start();
    }
    
    public void Stop()
    {
        stop = true;
        thread.Join();
    }

    public void CheckThread()
    {
        if (Thread.CurrentThread.ManagedThreadId != thread.ManagedThreadId)
            throw new InvalidOperationException($"Wrong thread {Thread.CurrentThread.ManagedThreadId}, expected {thread.ManagedThreadId}");
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        queue.Enqueue(new SynchronizationInfo(d, state, null));
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        ManualResetEvent ev = new ManualResetEvent(false);
        var info = new SynchronizationInfo(d, state, ev);
        queue.Enqueue(info);
        ev.WaitOne();
        var ex = Interlocked.Exchange(ref info.exception, null);
        if (ex != null)
            throw ex;
    }

    void ThreadRun(object? state)
    {
        while (!stop)
        {
            while (queue.TryDequeue(out SynchronizationInfo? info))
            {
                try
                {
                    info.callback(info.state);
                }
                catch (Exception ex)
                {
                    Interlocked.Exchange(ref info.exception, ex);
                    OnException?.Invoke(ex);
                }
                finally
                {
                    info.ev?.Set();
                }
            }
            Thread.Sleep(1);
        }
    }
}