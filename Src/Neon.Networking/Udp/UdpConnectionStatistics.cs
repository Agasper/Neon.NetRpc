using System;
using System.Text;
using System.Threading;

namespace Neon.Networking.Udp
{
    public class UdpConnectionStatistics : IConnectionStatistics
    {
        internal const int UNSTABLE_CONNECTION_TIMEOUT = 500;

        public bool UnstableConnection { get; private set; }
        public int? Latency => latency;
        public int? AvgLatency => avgLatency;
        public long PacketsOutTotal => packetsOutTotal;
        public long PacketsInTotal => packetsInTotal;
        public long BytesOutTotal => bytesOutTotal;
        public long BytesInTotal => bytesInTotal;

        public long PacketsOutSec => packetsOutSec;
        public long PacketsInSec => packetsInSec;
        public long BytesOutSec => bytesOutSec;
        public long BytesInSec => bytesInSec;

        long packetsOutTotal;
        long packetsInTotal;
        long bytesOutTotal;
        long bytesInTotal;

        long packetsOutSec;
        long packetsInSec;
        long bytesOutSec;
        long bytesInSec;

        long packetsOutSec_;
        long packetsInSec_;
        long bytesOutSec_;
        long bytesInSec_;

        DateTime lastUpdate;
        int? latency;
        int? avgLatency;

        public UdpConnectionStatistics()
        {
            
        }

        internal void Reset()
        {
            UnstableConnection = false;
            Interlocked.Exchange(ref packetsInTotal, 0);
            Interlocked.Exchange(ref packetsOutTotal, 0);
            Interlocked.Exchange(ref bytesInTotal, 0);
            Interlocked.Exchange(ref bytesOutTotal, 0);
            Interlocked.Exchange(ref packetsOutSec, 0);
            Interlocked.Exchange(ref packetsInSec, 0);
            Interlocked.Exchange(ref bytesOutSec, 0);
            Interlocked.Exchange(ref bytesInSec, 0);
            Interlocked.Exchange(ref packetsOutSec_, 0);
            Interlocked.Exchange(ref packetsInSec_, 0);
            Interlocked.Exchange(ref bytesOutSec_, 0);
            Interlocked.Exchange(ref bytesInSec_, 0);
        }

        internal void PollEvents()
        {
            if ((DateTime.UtcNow - lastUpdate).TotalSeconds < 1)
                return;

            lastUpdate = DateTime.UtcNow;
            Interlocked.Exchange(ref packetsOutSec, packetsOutSec_);
            Interlocked.Exchange(ref packetsInSec, packetsInSec_);
            Interlocked.Exchange(ref bytesOutSec, bytesOutSec_);
            Interlocked.Exchange(ref bytesInSec, bytesInSec_);
            Interlocked.Exchange(ref packetsOutSec_, 0);
            Interlocked.Exchange(ref packetsInSec_, 0);
            Interlocked.Exchange(ref bytesOutSec_, 0);
            Interlocked.Exchange(ref bytesInSec_, 0);
        }

        //internal void UpdateLatency(float latency, bool loss)
        //{
        //    this.Latency = latency;
        //    this.UnstableConnection = loss;
        //}

        internal void UpdateLatency(int newLatency, int newAvgLatency)
        {
            this.latency = newLatency;
            this.avgLatency = newAvgLatency;

            if (newLatency < UdpConnectionStatistics.UNSTABLE_CONNECTION_TIMEOUT)
                UnstableConnection = false;
            if (newLatency > UdpConnectionStatistics.UNSTABLE_CONNECTION_TIMEOUT)
                SetUnstable();
        }

        internal void SetUnstable()
        {
            UnstableConnection = true;
        }

        internal void PacketOut()
        {
            Interlocked.Increment(ref packetsOutTotal);
            Interlocked.Increment(ref packetsOutSec_);
        }

        internal void PacketIn()
        {
            Interlocked.Increment(ref packetsInTotal);
            Interlocked.Increment(ref packetsInSec_);
        }

        internal void BytesOut(int bytes)
        {
            Interlocked.Add(ref bytesOutTotal, bytes);
            Interlocked.Add(ref bytesOutSec_, bytes);
        }

        internal void BytesIn(int bytes)
        {
            Interlocked.Add(ref bytesInTotal, bytes);
            Interlocked.Add(ref bytesInSec_, bytes);
        }

        public override string ToString()
        {
            return this.ToString(true, true);
        }

        public string ToString(bool includeTotal, bool includeCurrent)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Latency: {0}{1}\n", Latency.HasValue?Latency.Value.ToString():"unknown", UnstableConnection ? " [LOSS]" : "");

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
            string[] suf = { " b", " kB", " MB", " GB", " TB", " PB", " EB" };
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }
    }
}
