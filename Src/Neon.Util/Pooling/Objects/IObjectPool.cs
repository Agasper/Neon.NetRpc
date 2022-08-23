using System;

namespace Neon.Util.Pooling.Objects
{
    public interface IObjectPool
    {
        object Pop(Type type, Func<object> generator);
        T Pop<T>(Func<T> generator);
        ObjectHolder<T> PopWithHolder<T>(Func<T> generator);
        void Return(object value);
        void Clear();
    }
}