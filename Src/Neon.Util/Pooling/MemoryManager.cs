using System;
using System.Buffers;
using Microsoft.IO;

namespace Neon.Util.Pooling
{
    public interface IMemoryManager
    {
        int DefaultBufferSize { get; }

        byte[] RentArray(int minimumLength);
        void ReturnArray(byte[] array, bool clearArray = false);

        RecyclableMemoryStream GetStream(Guid streamGuid);
        RecyclableMemoryStream GetStream(int length, Guid streamGuid);
        RecyclableMemoryStream GetStream(int length, bool contiguous, Guid streamGuid);
        RecyclableMemoryStream GetStream(ArraySegment<byte> segment, Guid streamGuid);
    }
    
    public class MemoryManager : IMemoryManager
    {
        public int DefaultBufferSize { get; set; } = 8192;
        
        readonly ArrayPool<byte> arrayPool;
        readonly RecyclableMemoryStreamManager streamManager;
        
        public static MemoryManager Shared
        {
            get
            {
                if (shared == null)
                {
                    lock (sharedLock)
                    {
                        if (shared == null)
                        {
                            shared = new MemoryManager();
                        }
                    }
                }

                return shared;
            }
        }
        
        static object sharedLock = new object();
        static MemoryManager shared;

        private MemoryManager()
        {
            arrayPool = ArrayPool<byte>.Shared;
            streamManager = new RecyclableMemoryStreamManager(1024, 1024, 1024 * 1024, true);
            streamManager.ThrowExceptionOnToArray = true;
        }

        public MemoryManager(ArrayPool<byte> arrayPool, RecyclableMemoryStreamManager streamManager)
        {
            this.arrayPool = arrayPool;
            this.streamManager = streamManager;
        }

        public byte[] RentArray(int minimumLength)
        {
            return arrayPool.Rent(minimumLength);
        }
        
        public void ReturnArray(byte[] array, bool clearArray = false)
        {
            arrayPool.Return(array, clearArray);
        }

        public RecyclableMemoryStream GetStream(Guid streamGuid)
        {
            return streamManager.GetStream(streamGuid) as RecyclableMemoryStream;
        }
        
        public RecyclableMemoryStream GetStream(int length, Guid streamGuid)
        {
            return streamManager.GetStream(streamGuid, string.Empty, length) as RecyclableMemoryStream;
        }
        
        public RecyclableMemoryStream GetStream(int length, bool contiguous, Guid streamGuid)
        {
            return streamManager.GetStream(streamGuid, string.Empty, length, contiguous) as RecyclableMemoryStream;
        }
        
        public RecyclableMemoryStream GetStream(ArraySegment<byte> segment, Guid streamGuid)
        {
            return streamManager.GetStream(streamGuid,string.Empty, segment.Array, segment.Offset, segment.Count) as RecyclableMemoryStream;
        }
    }
}