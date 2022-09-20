using System.Runtime.InteropServices;
using Mono.Unix;
using Mono.Unix.Native;

namespace Neon.ServerExample.Util
{
    /// <summary>
    /// Crossplatform class to handle POSIX signals and ^C in console
    /// </summary>
    public class SignalHelper : IDisposable
    {
        public bool IsSignaled { get; private set; }

        public delegate void DSignaled();

        readonly AutoResetEvent ev;
        Thread? signalThread;

        public SignalHelper()
        {
            ev = new AutoResetEvent(false);
            Console.CancelKeyPress += ConsoleOnCancelKeyPress; //For windows we must listen for ^C
        }

        public void Dispose()
        {
            ev.Dispose();
            Console.CancelKeyPress -= ConsoleOnCancelKeyPress;
        }

        void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            IsSignaled = true;
            ev.Set();
        }

        public void Start(DSignaled callback, params Signum[] signals)
        {
            if (signalThread != null)
                throw new InvalidOperationException("Signal thread already created");
            
            //creating a separate signal thread
            signalThread = new Thread(() =>
            {
                WaitForSignal(signals, callback);
                signalThread = null;
            });
            signalThread.Name = "SignalThread";
            signalThread.IsBackground = true;
            signalThread.Start(signals);
        }

        public void Start(DSignaled callback)
        {
            Start(callback, new Signum[] {Signum.SIGINT, Signum.SIGTERM});
        }

        public void Wait(params Signum[] signals)
        {
            WaitForSignal(signals, null);
        }

        public void Wait()
        {
            WaitForSignal(new Signum[] {Signum.SIGINT, Signum.SIGTERM}, null);
        }

        void WaitForSignal(Signum[] signums, DSignaled? callback)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //waiting for ^C for windows
                ev.WaitOne();
                callback?.Invoke();
                IsSignaled = true;
            }
            else
            {
                //waiting for signal on POSIX systems
                UnixSignal[] signals = new UnixSignal[signums.Length];
                for (int i = 0; i < signums.Length; i++)
                    signals[i] = new UnixSignal(signums[i]);
                UnixSignal.WaitAny(signals);
                callback?.Invoke();
                IsSignaled = true;
            }
        }
    }
}