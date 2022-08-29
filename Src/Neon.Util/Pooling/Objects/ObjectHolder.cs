using System;
namespace Neon.Util.Pooling.Objects
{
    /// <summary>
    /// An object wrapper, returning object on dispose
    /// </summary>
    public struct ObjectHolder<T> : IDisposable
    {
        /// <summary>
        /// Object
        /// </summary>
        /// <exception cref="ObjectDisposedException">If holder already disposed</exception>
        public T Object
        {
            get
            {
                if (!disposed)
                    return obj;
                throw new ObjectDisposedException($"Object holder <{typeof(T).Name}> already disposed. Object property can not be accessed");
            }
        }

        T obj;
        ObjectPool pool;
        volatile bool disposed;

        public ObjectHolder(ObjectPool pool, T obj)
        {
            this.pool = pool;
            this.obj = obj;
            this.disposed = false;
        }

        /// <summary>
        /// Disposes holder, returns object to the pool, prevents to use object further
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            pool.Return(obj);
        }

    }
}
