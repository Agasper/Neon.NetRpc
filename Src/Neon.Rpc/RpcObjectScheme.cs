using System;
using System.Collections.Generic;
using System.Reflection;
using Neon.Rpc.Messages;

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

    public class RpcObjectScheme
    {
        public RpcInvocationRules Rules { get; }
        /// <summary>
        /// Type of the entity
        /// </summary>
        public Type EntityType { get; private set; }
        
        /// <summary>
        /// List of methods available for invocation
        /// </summary>
        public IReadOnlyDictionary<string, MethodContainer> Methods => _remotingMethods;

        Dictionary<string, MethodContainer> _remotingMethods;

        private RpcObjectScheme(RpcInvocationRules rules)
        {
            Rules = rules;
        }

        public static RpcObjectScheme Create(RpcInvocationRules rules, Type entityType)
        {
            RpcObjectScheme scheme = new RpcObjectScheme(rules);
            scheme.EntityType = entityType;
            scheme._remotingMethods = new Dictionary<string, MethodContainer>();
            
            BindingFlags flags = BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance;
            if (rules.AllowNonPublicMethods)
                flags |= BindingFlags.NonPublic;

            foreach (var method in entityType
                         .GetMethods(flags))
            {
                var attr = method.GetCustomAttribute<RpcMethodAttribute>(true);

                if (attr == null)
                    continue;

                var methodContainer = new MethodContainer(method, rules.AllowLambdaExpressions);

                if (string.IsNullOrEmpty(attr.Name))
                    scheme._remotingMethods.Add(method.Name, methodContainer);
                else
                    scheme._remotingMethods.Add(attr.Name, methodContainer);
            }

            return scheme;
        }

        /// <summary>
        /// Get method container by the method identity
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Method not found</exception>
        public MethodContainer GetInvocationContainer(string method)
        {
            if (!_remotingMethods.TryGetValue(method, out var methodContainer))
                throw new RpcException(string.Format("Method `{0}` not found in type {1}", method, GetType().FullName), RpcResponseStatusCode.NotFound);

            return methodContainer;
        }
    }
}
