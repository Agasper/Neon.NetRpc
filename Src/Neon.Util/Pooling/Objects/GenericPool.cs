using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Neon.Util.Pooling.Objects
{
    /// <summary>
    /// A generic object pool
    /// </summary>
    public class GenericPool<T> : IDisposable
    {
        ConcurrentStack<T> stack;
        Func<T> generator;
        bool disposed;
        readonly int maxCount;

        public GenericPool(Func<T> generator, int initialCount, int maxCount)
        {
            this.maxCount = maxCount;
            this.generator = generator;
            if (initialCount > 0)
            {
                T[] initialValues = new T[initialCount];
                for (int i = 0; i < initialCount; i++)
                {
                    initialValues[i] = generator();
                }
                stack = new ConcurrentStack<T>(initialValues);
            }
            else
                stack = new ConcurrentStack<T>();
        }

        public GenericPool(Func<T> generator, int initialCount) : this(generator, initialCount, -1)
        {
        }

        private GenericPool(IEnumerable<T> initialValues)
        {
            stack = new ConcurrentStack<T>(initialValues);
        }

        private GenericPool()
        {
            stack = new ConcurrentStack<T>();
        }

        /// <summary>
        /// The amount of objects in the pool
        /// </summary>
        public int Count => stack.Count;

        /// <summary>
        /// Clearing the pool and disposing objects
        /// </summary>
        public void Clear()
        {
            bool isDisposable = typeof(IDisposable).IsAssignableFrom(typeof(T));

            if (isDisposable)
            {
                while(stack.TryPop(out T popped))
                {
                    (popped as IDisposable).Dispose();
                }
            }
            else
                stack.Clear();
        }

        /// <summary>
        /// Clearing the pool and disposing objects
        /// </summary>
        public void Dispose()
        {
            disposed = true;
            Clear();

            stack.Clear();
            stack = null;
        }

        T CreateNew()
        {
            if (generator == null)
                throw new InvalidOperationException("There is no more items");
            return generator();
        }

        /// <summary>
        /// Returns an object from the pool, if pool is empty creates a new object
        /// </summary>
        /// <returns>Object</returns>
        public T Pop()
        {
            CheckDisposed();
            if (stack.TryPop(out T result))
            {
                if (result is IPoolObject poolObject)
                    poolObject.OnTookFromPool();
                return result;
            }

            return CreateNew();
        }

        /// <summary>
        /// Returns an object from the pool
        /// </summary>
        /// <returns>true if we found an object, false if pool is empty</returns>
        public bool TryPop(out T value)
        {
            if (stack.TryPop(out value))
            {
                if (value is IPoolObject poolObject)
                    poolObject.OnTookFromPool();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns an object to the pool
        /// </summary>
        /// <param name="obj">Object to return</param>
        public void Return(T obj)
        {
            CheckDisposed();
            if (maxCount > 0 && stack.Count >= maxCount)
                return;
            if (obj is IPoolObject poolObject)
                poolObject.OnReturnToPool();
            stack.Push(obj);

        }

        void CheckDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(GenericPool<T>));
        }
    }
}
