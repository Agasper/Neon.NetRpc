using System;

namespace Neon.Rpc
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RpcMethodAttribute : Attribute
    {
        public string Name { get; private set; }

        public RpcMethodAttribute(string name)
        {
            Name = name;
        }

        public RpcMethodAttribute()
        {
            Name = null;
        }
    }
}
