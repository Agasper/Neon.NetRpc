using System;

namespace Neon.Util.Pooling
{
    public class RentedArray : IRentedArray
    {
        public byte[] Array
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(RentedArray));
                return _array;
            }
        }
        
        readonly byte[] _array;
        readonly MemoryManager _memoryManager;

        bool _disposed;
        string _allocationStack;

        internal RentedArray(byte[] array, MemoryManager memoryManager)
        {
            _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
            _array = array ?? throw new ArgumentNullException(nameof(array));
            if (memoryManager.GenerateCallStacks)
                _allocationStack = Environment.StackTrace;
        }
        
        public void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            _disposed = true;
            if (disposing)
                GC.SuppressFinalize(this);
            else
            {
                if (AppDomain.CurrentDomain.IsFinalizingForUnload())
                    return;

                _memoryManager.CallFinalizedEvent(new ObjectFinalizedEventArgs(Guid.Empty, typeof(RentedArray), _allocationStack));
            }

            _memoryManager.ReturnArray(_array);
        }
        
        ~RentedArray()
        {
            Dispose(false);
        }
        
        public void Dispose()
        {
            Dispose(true);
        }
    }
}