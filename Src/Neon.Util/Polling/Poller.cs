namespace Neon.Util.Polling
{
    public delegate void DPollEvents();

    /// <summary>
    /// A wrapper for polling thread with convenient control
    /// </summary>
    public sealed class Poller : Pollable
    {
        public new bool IsStarted => base.IsStarted;
        public int PollingThreadSleep
        {
            get => pollingThreadSleep;
            set => pollingThreadSleep = value;
        }

        public DPollEvents Delegate { get; set; }

        public Poller()
        {
        }

        public Poller(int pollingThreadSleep) : base(pollingThreadSleep)
        {
        }

        public Poller(int pollingThreadSleep, DPollEvents @delegate)
            : base(pollingThreadSleep)
        {
            this.Delegate = @delegate;
        }
        
        public new void StartPolling()
        {
            base.StartPolling();
        }

        public new void StopPolling(bool wait)
        {
            base.StopPolling(wait);
        }

        protected override void PollEventsInternal()
        {
            Delegate?.Invoke();
        }
    }
}
