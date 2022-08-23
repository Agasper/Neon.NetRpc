namespace Neon.Networking
{
    public interface IConnectionStatistics
    {
        int? Latency { get; }
        int? AvgLatency { get; }
        
        long PacketsOutTotal { get; }
        long PacketsInTotal { get; }
        long BytesOutTotal { get; }
        long BytesInTotal { get; }

        long PacketsOutSec { get; }
        long PacketsInSec { get; }
        long BytesOutSec { get; }
        long BytesInSec { get; }
    }
}