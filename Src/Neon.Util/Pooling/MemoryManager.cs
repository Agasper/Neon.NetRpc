using System;
using System.Buffers;
using Microsoft.IO;

namespace Neon.Util.Pooling
{
    /// <summary>
    /// Memory manager is designated to reuse memory chunks
    /// </summary>
    public interface IMemoryManager
    {
        /// <summary>
        /// Buffer size used for copying
        /// </summary>
        int DefaultBufferSize { get; }

        /// <summary>
        /// Rent an array for temporary use
        /// </summary>
        /// <param name="minimumLength">The minimum length of the array. Returning array may be bigger</param>
        /// <returns>Byte array</returns>
        byte[] RentArray(int minimumLength);
        
        /// <summary>
        /// Returns previously rented array
        /// </summary>
        /// <param name="array">The rented array</param>
        /// <param name="clearArray">Should we clear array</param>
        void ReturnArray(byte[] array, bool clearArray = false);

        /// <summary>
        /// Returns a temporary memory stream with a default size
        /// </summary>
        /// <param name="streamGuid">Unique stream identifier, used in debugging to catch undisposed streams</param>
        /// <returns>Stream</returns>
        RecyclableMemoryStream GetStream(Guid streamGuid);
        /// <summary>
        /// Returns a temporary memory stream
        /// </summary>
        /// <param name="length">The minimum length of the stream. Returning stream may be bigger</param>
        /// <param name="streamGuid">Unique stream identifier, used in debugging to catch undisposed streams</param>
        /// <returns>Stream</returns>
        RecyclableMemoryStream GetStream(int length, Guid streamGuid);
        /// <summary>
        /// Returns a temporary memory stream
        /// </summary>
        /// <param name="length">The minimum length of the stream. Returning stream may be bigger</param>
        /// <param name="contiguous">If false stream may be created from chunked memory (as default), if true contiguous chunk of memory will be allocated</param>
        /// <param name="streamGuid">Unique stream identifier, used in debugging to catch undisposed streams</param>
        /// <returns>Stream</returns>
        RecyclableMemoryStream GetStream(int length, bool contiguous, Guid streamGuid);
        /// <summary>
        /// Returns a temporary memory stream created from existing data
        /// </summary>
        /// <param name="segment">The data will be copied to the new stream</param>
        /// <param name="streamGuid">Unique stream identifier, used in debugging to catch undisposed streams</param>
        /// <returns>Stream</returns>
        RecyclableMemoryStream GetStream(ArraySegment<byte> segment, Guid streamGuid);
    }
    
    public class MemoryManager : IMemoryManager
    {
        /// <summary>
        /// Buffer size used for copying
        /// </summary>
        public int DefaultBufferSize { get; set; } = 8192;
        
        readonly ArrayPool<byte> arrayPool;
        readonly RecyclableMemoryStreamManager streamManager;
        
        /// <summary>
        /// A static predefined shared memory pool
        /// </summary>
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

        /// <summary>
        /// Rent an array for temporary use
        /// </summary>
        /// <param name="minimumLength">The minimum length of the array. Returning array may be bigger</param>
        /// <returns>Byte array</returns>
        public byte[] RentArray(int minimumLength)
        {
            return arrayPool.Rent(minimumLength);
        }
        
        /// <summary>
        /// Returns previously rented array
        /// </summary>
        /// <param name="array">The rented array</param>
        /// <param name="clearArray">Should we clear array</param>
        public void ReturnArray(byte[] array, bool clearArray = false)
        {
            arrayPool.Return(array, clearArray);
        }

        /// <summary>
        /// Returns a temporary memory stream with a default size
        /// </summary>
        /// <param name="streamGuid">Unique stream identifier, used in debugging to catch undisposed streams</param>
        /// <returns>Stream</returns>
        public RecyclableMemoryStream GetStream(Guid streamGuid)
        {
            return streamManager.GetStream(streamGuid) as RecyclableMemoryStream;
        }
        
        /// <summary>
        /// Returns a temporary memory stream
        /// </summary>
        /// <param name="length">The minimum length of the stream. Returning stream may be bigger</param>
        /// <param name="streamGuid">Unique stream identifier, used in debugging to catch undisposed streams</param>
        /// <returns>Stream</returns>
        public RecyclableMemoryStream GetStream(int length, Guid streamGuid)
        {
            return streamManager.GetStream(streamGuid, string.Empty, length) as RecyclableMemoryStream;
        }
        
        /// <summary>
        /// Returns a temporary memory stream
        /// </summary>
        /// <param name="length">The minimum length of the stream. Returning stream may be bigger</param>
        /// <param name="contiguous">If false stream may be created from chunked memory (as default), if true contiguous chunk of memory will be allocated</param>
        /// <param name="streamGuid">Unique stream identifier, used in debugging to catch undisposed streams</param>
        /// <returns>Stream</returns>
        public RecyclableMemoryStream GetStream(int length, bool contiguous, Guid streamGuid)
        {
            return streamManager.GetStream(streamGuid, string.Empty, length, contiguous) as RecyclableMemoryStream;
        }
        
        /// <summary>
        /// Returns a temporary memory stream created from existing data
        /// </summary>
        /// <param name="segment">The data will be copied to the new stream</param>
        /// <param name="streamGuid">Unique stream identifier, used in debugging to catch undisposed streams</param>
        /// <returns>Stream</returns>
        public RecyclableMemoryStream GetStream(ArraySegment<byte> segment, Guid streamGuid)
        {
            return streamManager.GetStream(streamGuid,string.Empty, segment.Array, segment.Offset, segment.Count) as RecyclableMemoryStream;
        }
    }
}