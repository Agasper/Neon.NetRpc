using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Neon.Util.Pooling.Objects
{
    public class ObjectPool : IObjectPool
    {
        public static ObjectPool Shared => shared;

        static ObjectPool shared = new ObjectPool();

        ConcurrentDictionary<Type, ConcurrentBag<object>> cache;
        readonly int maxObjectsByType = -1;

        public ObjectPool(int maxObjectsByType)
        {
            this.maxObjectsByType = maxObjectsByType;
        }

        public void Clear()
        {
            cache.Clear();
        }

        public ObjectPool()
        {
            cache = new ConcurrentDictionary<Type, ConcurrentBag<object>>();
        }

        public Dictionary<Type, int> GetStatsByType()
        {
            Dictionary<Type, int> result = new Dictionary<Type, int>();
            foreach(var pair in cache)
            {
                result.Add(pair.Key, pair.Value.Count);
            }
            return result;
        }

        public object Pop(Type type, Func<object> generator)
        {
            ConcurrentBag<object> bag = cache.GetOrAdd(type, (t) => new ConcurrentBag<object>()); ;

            if (bag.TryTake(out object result))
            {
                if (result is IPoolObject poolObject)
                    poolObject.OnTookFromPool();
                return result;
            }
            else
            {
                return generator();
            }
        }

        public T Pop<T>(Func<T> generator)
        {
            ConcurrentBag<object> bag = cache.GetOrAdd(typeof(T), (t) => new ConcurrentBag<object>());

            if (bag.TryTake(out object result))
            {
                if (result is IPoolObject poolObject)
                    poolObject.OnTookFromPool();
                return (T)result;
            }
            else
            {
                return generator();
            }
        }
        
        public T Pop<T>() where T : new()
        {
            return Pop<T>(() => new T());
        }

        public ObjectHolder<T> PopWithHolder<T>(Func<T> generator)
        {
            ConcurrentBag<object> bag = cache.GetOrAdd(typeof(T), (t) => new ConcurrentBag<object>());

            if (bag.TryTake(out object result))
            {
                if (result is IPoolObject poolObject)
                    poolObject.OnTookFromPool();
                return new ObjectHolder<T>(this, (T)result);
            }
            else
            {
                return new ObjectHolder<T>(this, generator());
            }
        }

        public void Return(IEnumerable<object> values)
        {
            foreach(var value in values)
            {
                Return(value);
            }
        }

        public void Return(object value)
        {
            Type type = value.GetType();
            ConcurrentBag<object> bag = cache.GetOrAdd(type, (t) => new ConcurrentBag<object>());
            if (maxObjectsByType > 0 && bag.Count >= maxObjectsByType)
                return;
            if (value is IPoolObject poolObject)
                poolObject.OnReturnToPool();
            bag.Add(value);
        }
    }
}
