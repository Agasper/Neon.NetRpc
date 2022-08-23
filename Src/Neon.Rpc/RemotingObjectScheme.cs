using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Neon.Rpc
{
    public struct RemotingInvocationRules
    {
        public bool AllowAsync { get; set; }
        public bool AllowNonVoid { get; set; }
        public bool AllowLambdaExpressions { get; set; }
        public bool AllowNonPublicMethods { get; set; }

        public static RemotingInvocationRules Default =>
            new RemotingInvocationRules()
            {
                AllowAsync = true,
                AllowLambdaExpressions = true,
                AllowNonVoid = true,
                AllowNonPublicMethods = true
            };
    }

    public class RemotingObjectScheme
    {
        public Type EntityType { get; }
        public IReadOnlyDictionary<object, MethodContainer> Methods => remotingMethods;

        Dictionary<object, MethodContainer> remotingMethods;

        public RemotingObjectScheme(RemotingInvocationRules rules, Type entityType)
        {
            this.EntityType = entityType;
            Init(rules, entityType);
        }

        public MethodContainer GetInvocationContainer(object key)
        {
            MethodContainer methodContainer = null;

            if (!remotingMethods.ContainsKey(key))
                throw new ArgumentException(string.Format("Method key `{0}` not found in type {1}", key, this.GetType().Name));
            methodContainer = remotingMethods[key];

            return methodContainer;
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
                var asyncAttr = method.GetCustomAttribute<AsyncStateMachineAttribute>(true);

                if (attr == null)
                    continue;

                var isReturnGenericTask = method.ReturnType.IsGenericType &&
                    method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
                var isReturnTask = method.ReturnType == typeof(Task);

                if (!configuration.AllowAsync && (asyncAttr != null || isReturnGenericTask || isReturnTask))
                    throw new TypeLoadException($"Async method {method.Name} not allowed in {entityType.FullName}");

                if (!configuration.AllowNonVoid && method.ReturnType != typeof(void))
                    throw new TypeLoadException($"Non void method {method.Name} not allowed in {entityType.FullName}");

                if (asyncAttr != null && method.ReturnType == typeof(void))
                    throw new TypeLoadException($"Async void methods not allowed: {entityType.FullName}, {method.Name}. Please change it to async Task.");

                switch (attr.MethodIdentityType)
                {
                    case RemotingMethodAttribute.MethodIdentityTypeEnum.ByIndex:
                        myRemotingMethods2.Add(attr.Index, new MethodContainer(method, configuration.AllowLambdaExpressions));
                        break;
                    case RemotingMethodAttribute.MethodIdentityTypeEnum.ByName:
                        myRemotingMethods2.Add(attr.Name, new MethodContainer(method, configuration.AllowLambdaExpressions));
                        break;
                    case RemotingMethodAttribute.MethodIdentityTypeEnum.Default:
                        myRemotingMethods2.Add(method.Name, new MethodContainer(method, configuration.AllowLambdaExpressions));
                        break;
                    default:
                        throw new ArgumentException($"Could not get method identification for {method.Name}");
                }
            }

            this.remotingMethods = myRemotingMethods2;
        }
    }
}
