using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Neon.Util.Pooling.Objects
{
    /// <summary>
    /// Non generic object pool
    /// </summary>
    public class ObjectPool : IObjectPool
    {
        /// <summary>
        /// Shared object pool
        /// </summary>
        public static ObjectPool Shared => shared;

        static ObjectPool shared = new ObjectPool();

        ConcurrentDictionary<Type, ConcurrentBag<object>> cache;
        readonly int maxObjectsByType = -1;

        public ObjectPool(int maxObjectsByType)
        {
            this.maxObjectsByType = maxObjectsByType;
        }

        /// <summary>
        /// Clearing the pool
        /// </summary>
        public void Clear()
        {
            cache.Clear();
        }

        public ObjectPool()
        {
            cache = new ConcurrentDictionary<Type, ConcurrentBag<object>>();
        }

        /// <summary>
        /// Returns amount of every type presents in the pool
        /// </summary>
        /// <returns></returns>
        public Dictionary<Type, int> GetStatsByType()
        {
            Dictionary<Type, int> result = new Dictionary<Type, int>();
            foreach(var pair in cache)
            {
                result.Add(pair.Key, pair.Value.Count);
            }
            return result;
        }

        /// <summary>
        /// Returns an object from the pool, if pool is empty creates a new object with a generator function
        /// </summary>
        /// <param name="type">Object type</param>
        /// <param name="generator">Function returning a new object if not found in the pool</param>
        /// <returns>An instance of requested object</returns>
        public object Pop(Type type, Func<object> generator)
        {
            ConcurrentBag<object> bag = cache.GetOrAdd(type, t => new ConcurrentBag<object>()); ;

            if (bag.TryTake(out object result))
            {
                if (result is IPoolObject poolObject)
                    poolObject.OnTookFromPool();
                return result;
            }

            return generator();
        }

        /// <summary>
        /// Returns an object from the pool, if pool is empty creates a new object with a generator function
        /// </summary>
        /// <param name="generator">Function returning a new object if not found in the pool</param>
        /// <returns>An instance of requested object</returns>
        public T Pop<T>(Func<T> generator)
        {
            ConcurrentBag<object> bag = cache.GetOrAdd(typeof(T), t => new ConcurrentBag<object>());

            if (bag.TryTake(out object result))
            {
                if (result is IPoolObject poolObject)
                    poolObject.OnTookFromPool();
                return (T)result;
            }

            return generator();
        }
        
        /// <summary>
        /// Returns an object from the pool, if pool is empty creates a new object with new()
        /// </summary>
        /// <returns>An instance of requested object</returns>
        public T Pop<T>() where T : new()
        {
            return Pop(() => new T());
        }

        /// <summary>
        /// Returns an object from the pool, if pool is empty creates a new object with a generator function
        /// </summary>
        /// <param name="generator">Function returning a new object if not found in the pool</param>
        /// <returns>An instance of object holder</returns>
        public ObjectHolder<T> PopWithHolder<T>(Func<T> generator)
        {
            ConcurrentBag<object> bag = cache.GetOrAdd(typeof(T), t => new ConcurrentBag<object>());

            if (bag.TryTake(out object result))
            {
                if (result is IPoolObject poolObject)
                    poolObject.OnTookFromPool();
                return new ObjectHolder<T>(this, (T)result);
            }

            return new ObjectHolder<T>(this, generator());
        }

        /// <summary>
        /// Returns objects to the pool
        /// </summary>
        /// <param name="values">Enumerator of objects</param>
        public void Return(IEnumerable<object> values)
        {
            foreach(var value in values)
            {
                Return(value);
            }
        }

        /// <summary>
        /// Returns object to the pool
        /// </summary>
        /// <param name="value">Object to return</param>
        public void Return(object value)
        {
            Type type = value.GetType();
            ConcurrentBag<object> bag = cache.GetOrAdd(type, t => new ConcurrentBag<object>());
            if (maxObjectsByType > 0 && bag.Count >= maxObjectsByType)
                return;
            if (value is IPoolObject poolObject)
                poolObject.OnReturnToPool();
            bag.Add(value);
        }
    }
}
