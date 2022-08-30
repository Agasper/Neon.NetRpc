using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Neon.Rpc
{
    public struct RemotingInvocationRules
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

        public static RemotingInvocationRules Default =>
            new RemotingInvocationRules()
            {
                // AllowAsync = true,
                AllowLambdaExpressions = true,
                // AllowNonVoid = true,
                AllowNonPublicMethods = true
            };
    }

    public class RemotingObjectScheme
    {
        /// <summary>
        /// Type of the entity
        /// </summary>
        public Type EntityType { get; }
        
        /// <summary>
        /// List of methods available for invocation
        /// </summary>
        public IReadOnlyDictionary<object, MethodContainer> Methods => remotingMethods;

        Dictionary<object, MethodContainer> remotingMethods;

        public RemotingObjectScheme(RemotingInvocationRules rules, Type entityType)
        {
            this.EntityType = entityType;
            Init(rules, entityType);
        }

        /// <summary>
        /// Get method container by the method identity
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Method not found</exception>
        public MethodContainer GetInvocationContainer(object methodIdentity)
        {
            MethodContainer methodContainer = null;

            if (!remotingMethods.ContainsKey(methodIdentity))
                throw new ArgumentException(string.Format("Method key `{0}` not found in type {1}", methodIdentity, this.GetType().Name));
            methodContainer = remotingMethods[methodIdentity];

            return methodContainer;
        }

        protected virtual bool OnValidateMethod(RemotingMethodAttribute attribute, MethodContainer methodContainer)
        {
            return true;
        }

        void Init(RemotingInvocationRules configuration, Type entityType)
        {
            Dictionary<object, MethodContainer> myRemotingMethods2 = new Dictionary<object, MethodContainer>();

            BindingFlags flags = BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance;
            if (configuration.AllowNonPublicMethods)
                flags |= BindingFlags.NonPublic;

            foreach (var method in entityType
                         .GetMethods(flags))
            {
                var attr = method.GetCustomAttribute<RemotingMethodAttribute>(true);
                // var asyncAttr = method.GetCustomAttribute<AsyncStateMachineAttribute>(true);

                if (attr == null)
                    continue;

                // var isReturnGenericTask = method.ReturnType.IsGenericType &&
                //     method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
                // var isReturnTask = method.ReturnType == typeof(Task);
                //
                // if (!configuration.AllowAsync && (asyncAttr != null || isReturnGenericTask || isReturnTask))
                //     throw new TypeLoadException($"Async method {method.Name} not allowed in {entityType.FullName}");
                //
                // if (!configuration.AllowNonVoid && method.ReturnType != typeof(void))
                //     throw new TypeLoadException($"Non void method {method.Name} not allowed in {entityType.FullName}");
                //
                // if (asyncAttr != null && method.ReturnType == typeof(void))
                //     throw new TypeLoadException($"Async void methods not allowed: {entityType.FullName}, {method.Name}. Please change it to async Task.");

                var methodContainer = new MethodContainer(method, configuration.AllowLambdaExpressions);
                if (OnValidateMethod(attr, methodContainer))
                {
                    switch (attr.MethodIdentityType)
                    {
                        case RemotingMethodAttribute.MethodIdentityTypeEnum.ByIndex:
                            myRemotingMethods2.Add(attr.Index, methodContainer);
                            break;
                        case RemotingMethodAttribute.MethodIdentityTypeEnum.ByName:
                            myRemotingMethods2.Add(attr.Name, methodContainer);
                            break;
                        case RemotingMethodAttribute.MethodIdentityTypeEnum.Default:
                            myRemotingMethods2.Add(method.Name, methodContainer);
                            break;
                        default:
                            throw new ArgumentException($"Could not get method identification for {method.Name}");
                    }
                }
            }

            this.remotingMethods = myRemotingMethods2;
        }
    }
}
