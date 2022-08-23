using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Neon.Rpc
{
    class SynchronizationContextTaskScheduler : TaskScheduler
    {
        readonly ConcurrentQueue<Task> _tasks;
        readonly SynchronizationContext _context;

        public SynchronizationContextTaskScheduler() :
            this(SynchronizationContext.Current)
        {
        }

        public SynchronizationContextTaskScheduler(SynchronizationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            _context = context;
            _tasks = new ConcurrentQueue<Task>();
        }

        /// <summary>Queues a task to the scheduler for execution on the I/O ThreadPool.</summary>
        /// <param name="task">The Task to queue.</param>
        protected override void QueueTask(Task task)
        {
            _tasks.Enqueue(task);
            _context.Post(PostCallback, _tasks);
        }
        
        // preallocated SendOrPostCallback delegate

        // this is where the actual task invocation occures
        void PostCallback(object obj)
        {
            // Task task = (Task) obj;
            ConcurrentQueue<Task> _tasks = (ConcurrentQueue<Task>) obj;
            Task nextTask;
            if (_tasks.TryDequeue(out nextTask)) 
                TryExecuteTask(nextTask);
        }

        /// <summary>Tries to execute a task on the current thread.</summary>
        /// <param name="task">The task to be executed.</param>
        /// <param name="taskWasPreviouslyQueued">Ignored.</param>
        /// <returns>Whether the task could be executed.</returns>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return _context == SynchronizationContext.Current && TryExecuteTask(task);
        }

        /// <summary>Gets an enumerable of tasks queued to the scheduler.</summary>
        /// <returns>An enumerable of tasks queued to the scheduler.</returns>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks.ToArray();
        }

        /// <summary>Gets the maximum concurrency level supported by this scheduler.</summary>
        public override int MaximumConcurrencyLevel { get { return 1; } }
    }
}