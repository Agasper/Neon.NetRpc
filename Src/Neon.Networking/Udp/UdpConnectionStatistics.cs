using System;
using System.Text;
using System.Threading;

namespace Neon.Networking.Udp
{
    public class UdpConnectionStatistics : IConnectionStatistics
    {
        internal const int UNSTABLE_CONNECTION_TIMEOUT = 500;

        public bool UnstableConnection { get; private set; }
        long _bytesInSec;
        long _bytesInSec_;
        long _bytesInTotal;
        long _bytesOutSec;
        long _bytesOutSec_;
        long _bytesOutTotal;

        DateTime _lastUpdate;
        long _packetsInSec;
        long _packetsInSec_;
        long _packetsInTotal;

        long _packetsOutSec;

        long _packetsOutSec_;

        long _packetsOutTotal;
        public int? Latency { get; private set; }

        public int? AvgLatency { get; private set; }

        public long PacketsOutTotal => _packetsOutTotal;
        public long PacketsInTotal => _packetsInTotal;
        public long BytesOutTotal => _bytesOutTotal;
        public long BytesInTotal => _bytesInTotal;

        public long PacketsOutSec => _packetsOutSec;
        public long PacketsInSec => _packetsInSec;
        public long BytesOutSec => _bytesOutSec;
        public long BytesInSec => _bytesInSec;

        internal void Reset()
        {
            UnstableConnection = false;
            Interlocked.Exchange(ref _packetsInTotal, 0);
            Interlocked.Exchange(ref _packetsOutTotal, 0);
            Interlocked.Exchange(ref _bytesInTotal, 0);
            Interlocked.Exchange(ref _bytesOutTotal, 0);
            Interlocked.Exchange(ref _packetsOutSec, 0);
            Interlocked.Exchange(ref _packetsInSec, 0);
            Interlocked.Exchange(ref _bytesOutSec, 0);
            Interlocked.Exchange(ref _bytesInSec, 0);
            Interlocked.Exchange(ref _packetsOutSec_, 0);
            Interlocked.Exchange(ref _packetsInSec_, 0);
            Interlocked.Exchange(ref _bytesOutSec_, 0);
            Interlocked.Exchange(ref _bytesInSec_, 0);
        }

        internal void PollEvents()
        {
            if ((DateTime.UtcNow - _lastUpdate).TotalSeconds < 1)
                return;

            _lastUpdate = DateTime.UtcNow;
            Interlocked.Exchange(ref _packetsOutSec, _packetsOutSec_);
            Interlocked.Exchange(ref _packetsInSec, _packetsInSec_);
            Interlocked.Exchange(ref _bytesOutSec, _bytesOutSec_);
            Interlocked.Exchange(ref _bytesInSec, _bytesInSec_);
            Interlocked.Exchange(ref _packetsOutSec_, 0);
            Interlocked.Exchange(ref _packetsInSec_, 0);
            Interlocked.Exchange(ref _bytesOutSec_, 0);
            Interlocked.Exchange(ref _bytesInSec_, 0);
        }

        //internal void UpdateLatency(float latency, bool loss)
        //{
        //    this.Latency = latency;
        //    this.UnstableConnection = loss;
        //}

        internal void UpdateLatency(int newLatency, int newAvgLatency)
        {
            Latency = newLatency;
            AvgLatency = newAvgLatency;

            if (newLatency < UNSTABLE_CONNECTION_TIMEOUT)
                UnstableConnection = false;
            if (newLatency > UNSTABLE_CONNECTION_TIMEOUT)
                SetUnstable();
        }

        internal void SetUnstable()
        {
            UnstableConnection = true;
        }

        internal void PacketOut()
        {
            Interlocked.Increment(ref _packetsOutTotal);
            Interlocked.Increment(ref _packetsOutSec_);
        }

        internal void PacketIn()
        {
            Interlocked.Increment(ref _packetsInTotal);
            Interlocked.Increment(ref _packetsInSec_);
        }

        internal void BytesOut(int bytes)
        {
            Interlocked.Add(ref _bytesOutTotal, bytes);
            Interlocked.Add(ref _bytesOutSec_, bytes);
        }

        internal void BytesIn(int bytes)
        {
            Interlocked.Add(ref _bytesInTotal, bytes);
            Interlocked.Add(ref _bytesInSec_, bytes);
        }

        public override string ToString()
        {
            return ToString(true, true);
        }

        public string ToString(bool includeTotal, bool includeCurrent)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Latency: {0}{1}\n", Latency.HasValue ? Latency.Value.ToString() : "unknown",
                UnstableConnection ? " [LOSS]" : "");

            if (includeCurrent)
            {
                sb.AppendFormat("PpsOut: {0}\n", PacketsOutSec);
                sb.AppendFormat("PpsIn: {0}\n", PacketsInSec);
                sb.AppendFormat("RateOut: {0}/s\n", BytesToString(BytesOutSec));
                sb.AppendFormat("RateIn: {0}/s", BytesToString(BytesInSec));
            }

            if (includeTotal)
            {
                sb.AppendFormat("\nPacketsOut: {0}\n", PacketsOutTotal);
                sb.AppendFormat("PacketsIn: {0}\n", PacketsInTotal);
                sb.AppendFormat("TotalOut: {0}\n", BytesToString(BytesOutTotal));
                sb.AppendFormat("TotalIn: {0}", BytesToString(BytesInTotal));
            }

            return sb.ToString();
        }

        static string BytesToString(long byteCount)
        {
            string[] suf = {" b", " kB", " MB", " GB", " TB", " PB", " EB"};
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return Math.Sign(byteCount) * num + suf[place];
        }
    }
}