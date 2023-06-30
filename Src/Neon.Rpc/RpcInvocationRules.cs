using System;

namespace Neon.Rpc
{
    public struct RpcInvocationRules : IEquatable<RpcInvocationRules>
    {
        // public bool AllowAsync { get; set; }
        // public bool AllowNonVoid { get; set; }
        /// <summary>
        /// Enables optimization of delegate building via lambda expressions
        /// </summary>
        public bool AllowLambdaExpressions { get; set; }
        /// <summary>
        /// If enabled we will add non-public methods to the invocation list 
        /// </summary>
        public bool AllowNonPublicMethods { get; set; }

        public static RpcInvocationRules Default =>
            new RpcInvocationRules
            {
                // AllowAsync = true,
                AllowLambdaExpressions = true,
                // AllowNonVoid = true,
                AllowNonPublicMethods = true
            };

        public bool Equals(RpcInvocationRules other)
        {
            return AllowLambdaExpressions == other.AllowLambdaExpressions && AllowNonPublicMethods == other.AllowNonPublicMethods;
        }

        public override bool Equals(object obj)
        {
            return obj is RpcInvocationRules other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (AllowLambdaExpressions.GetHashCode() * 397) ^ AllowNonPublicMethods.GetHashCode();
            }
        }
    }
}