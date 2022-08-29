using System;
using System.Collections.Generic;

namespace Neon.Util.Pooling.Objects
{
    public interface IObjectPool
    {
        /// <summary>
        /// Returns an object from the pool, if pool is empty creates a new object with a generator function
        /// </summary>
        /// <param name="type">Object type</param>
        /// <param name="generator">Function returning a new object if not found in the pool</param>
        /// <returns>An instance of requested object</returns>
        object Pop(Type type, Func<object> generator);

        /// <summary>
        /// Returns an object from the pool, if pool is empty creates a new object with a generator function
        /// </summary>
        /// <param name="generator">Function returning a new object if not found in the pool</param>
        /// <returns>An instance of requested object</returns>
        T Pop<T>(Func<T> generator);
        
        /// <summary>
        /// Returns an object from the pool, if pool is empty creates a new object with new()
        /// </summary>
        /// <returns>An instance of requested object</returns>
        T Pop<T>() where T : new();
        
        /// <summary>
        /// Returns an object from the pool, if pool is empty creates a new object with a generator function
        /// </summary>
        /// <param name="generator">Function returning a new object if not found in the pool</param>
        /// <returns>An instance of object holder</returns>
        ObjectHolder<T> PopWithHolder<T>(Func<T> generator);

        /// <summary>
        /// Returns objects to the pool
        /// </summary>
        /// <param name="values">Enumerator of objects</param>
        void Return(IEnumerable<object> values);
        
        /// <summary>
        /// Returns object to the pool
        /// </summary>
        /// <param name="value">Object to return</param>
        void Return(object value);
        
        /// <summary>
        /// Clearing the pool
        /// </summary>
        void Clear();
    }
}