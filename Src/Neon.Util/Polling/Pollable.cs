using System;
using System.Threading;

namespace Neon.Util.Polling
{
    public abstract class Pollable
    {
        protected bool IsStarted => pollingIsRun || pollingThread != null;
        protected int pollingThreadSleep;

        Thread pollingThread;
        bool pollingIsRun;

        public Pollable()
        {
            pollingThreadSleep = 5;
        }

        public Pollable(int pollingThreadSleep)
        {
            this.pollingThreadSleep = pollingThreadSleep;
        }

        protected void StartPolling()
        {
            if (pollingThread != null || pollingIsRun)
                throw new InvalidOperationException("Polling thread already started");

            pollingIsRun = true;
            pollingThread = new Thread(PollThreadRun);
            pollingThread.IsBackground = true;
            pollingThread.Start();
        }

        protected virtual void StopPolling(bool wait)
        {
            var pollingThread_ = pollingThread;
            if (pollingThread_ == null)
                throw new InvalidOperationException("Polling isn't started");
            pollingThread = null;
            pollingIsRun = false;

            if (wait)
                pollingThread_.Join();

        }

        void PollThreadRun()
        {
            while (pollingIsRun)
            {
                this.PollEventsInternal();
                Thread.Sleep(pollingThreadSleep);
            }

            this.PollEventsInternal();
        }

        private protected void PollEvents()
        {
            if (pollingIsRun)
                throw new InvalidOperationException($"If you start polling thread, no need to call {nameof(PollEvents)}");

            PollEventsInternal();
        }

        protected abstract void PollEventsInternal();
    }
}
