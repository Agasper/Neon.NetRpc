using System;

namespace Neon.Util.Pooling
{
    public interface IRentedArray : IDisposable
    {
        byte[] Array { get; }
    }
}