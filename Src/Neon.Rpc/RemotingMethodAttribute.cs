using System;
namespace Neon.Rpc
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RemotingMethodAttribute : Attribute
    {
        internal enum MethodIdentityTypeEnum
        {
            ByName,
            ByIndex,
            Default
        }

        public string Name { get; private set; }
        public int Index { get; private set; }
        internal MethodIdentityTypeEnum MethodIdentityType { get; private set; }

        public RemotingMethodAttribute(string name)
        {
            MethodIdentityType = MethodIdentityTypeEnum.ByName;
            this.Name = name;
            this.Index = 0;
        }

        public RemotingMethodAttribute(int index)
        {
            MethodIdentityType = MethodIdentityTypeEnum.ByIndex;
            this.Name = null;
            this.Index = index;
        }

        public RemotingMethodAttribute()
        {
            MethodIdentityType = MethodIdentityTypeEnum.Default;
            this.Name = null;
            this.Index = 0;
        }
    }
}
