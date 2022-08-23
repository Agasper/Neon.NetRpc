using System;
namespace Neon.Util.Pooling.Objects
{
    public struct ObjectHolder<T> : IDisposable
    {
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
        bool disposed;

        public ObjectHolder(ObjectPool pool, T obj)
        {
            this.pool = pool;
            this.obj = obj;
            this.disposed = false;
        }

        public void Dispose()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ObjectHolder<T>));
            disposed = true;
            pool.Return(obj);
        }

    }
}
