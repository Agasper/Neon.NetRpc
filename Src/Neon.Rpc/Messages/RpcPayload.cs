using System;
using Neon.Util.Pooling;

namespace Neon.Rpc.Messages
{
    interface IRpcPayload
    {
        byte[] Array { get; }
        int Size { get; }
    }
    
    class RpcPayload : IRpcPayload, IDisposable
    {
        public byte[] Array
        {
            get
            {
                CheckDisposed();
                return _rentedArray.Array;
            }
        }

        public int Size
        {
            get
            {
                CheckDisposed();
                return _size;
            }
        }

        IRentedArray _rentedArray;
        int _size;
        bool _disposed;

        public RpcPayload(IRentedArray rentedArray, int size)
        {
            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Size can't be less than zero");
            _rentedArray = rentedArray;
            _size = size;
        }

        public void CopyTo(RpcPayload payload)
        {
            CheckDisposed();
            System.Array.Copy(Array, 0, payload.Array, 0, _size);
            payload._size = _size;
        }

        void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RpcPayload));
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _rentedArray.Dispose();
            _size = 0;
        }
    }
}