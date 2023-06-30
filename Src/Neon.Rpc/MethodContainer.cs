using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Neon.Rpc.Messages;

namespace Neon.Rpc
{
    
    /// <summary>
    /// Container optimizes any method execution with a lambda expressions
    /// </summary>
    class MethodContainer
    {
        delegate object DInvokeDelegate(object instance, IMessage arg, CancellationToken cancellationToken);
        delegate IMessage DGetTaskResult(Task task);

        /// <summary>
        /// Method argument descriptor
        /// </summary>
        public MessageDescriptor ArgumentDescriptor { get; }
        
        /// <summary>
        /// Method result descriptor
        /// </summary>
        public MessageDescriptor ResultDescriptor { get; }


        readonly bool _doesReturnTask;
        readonly bool _doesReturnValue;
        readonly bool _isVoidMethod;
        readonly bool _useLambda;
        readonly MethodInfo _methodInfo;
        readonly ParameterInfo[] _parameters;
        readonly Type _returnType;
        readonly Type _returnTypeValue;
        readonly int argumentIndex = -1;
        readonly int cancellationTokenIndex = -1;

        Type _declaringType;
        DGetTaskResult _getTaskResultDelegate;
        DInvokeDelegate _invokeDelegate;

        public MethodContainer(MethodInfo methodInfo, bool useLambda)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));
            if (methodInfo.DeclaringType == null)
                throw new RpcException($"Declaring type of method {methodInfo.DeclaringType?.Name ?? ""}.{methodInfo.Name} is null", RpcResponseStatusCode.InvalidArgument);

            _declaringType = methodInfo.DeclaringType;
            _methodInfo = methodInfo;
            _useLambda = useLambda;

            _parameters = methodInfo.GetParameters();
            for (int i = 0; i < _parameters.Length; i++)
            {
                ParameterInfo p = _parameters[i];
                if (p.ParameterType == typeof(CancellationToken))
                {
                    if (cancellationTokenIndex >= 0)
                        throw new RpcException($"Method {_declaringType.FullName}.{methodInfo.Name} has more than one CancellationToken parameter", RpcResponseStatusCode.InvalidArgument);
                    cancellationTokenIndex = i;
                }
                else if (typeof(IMessage).IsAssignableFrom(p.ParameterType))
                {
                    if (argumentIndex >= 0)
                        throw new RpcException($"Method {_declaringType.FullName}.{methodInfo.Name} has more than one argument", RpcResponseStatusCode.InvalidArgument);
                    argumentIndex = i;
                }
                else
                    throw new RpcException($"Parameter ({p.ParameterType.Name} {p.Name}) of method {_declaringType.FullName}.{methodInfo.Name} is not supported.", RpcResponseStatusCode.InvalidArgument);
            }

            if (argumentIndex >= 0)
            {
                ArgumentDescriptor = GetDescriptor(_parameters[argumentIndex].ParameterType);
            }

            bool doesReturnGenericTask = methodInfo.ReturnType.IsGenericType &&
                                         methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
            
            _doesReturnTask = doesReturnGenericTask || methodInfo.ReturnType == typeof(Task);

            _doesReturnValue = methodInfo.ReturnType != typeof(void) &&
                methodInfo.ReturnType != typeof(Task);
            _isVoidMethod = methodInfo.ReturnType == typeof(void);

            _returnType = methodInfo.ReturnType;

            if (doesReturnGenericTask)
                _returnTypeValue = _returnType.GetGenericArguments()[0];
            else if (_doesReturnValue)
                _returnTypeValue = _returnType;

            if (_returnTypeValue != null && !_doesReturnTask && !doesReturnGenericTask && !typeof(IMessage).IsAssignableFrom(_returnTypeValue) )
                throw new RpcException($"Return type {_returnTypeValue.Name} of method {_declaringType.FullName}.{methodInfo.Name} is not supported.", RpcResponseStatusCode.InvalidArgument);

            if (_returnTypeValue != null)
            {
                ResultDescriptor = GetDescriptor(_returnTypeValue);
            }

            if (useLambda)
                CreateDelegate();
            else
                CreateDelegateNoLambda();

            if (_doesReturnTask && _doesReturnValue)
            {
                if (useLambda)
                    CreateTaskResultDelegate();
                else
                    CreateTaskResultDelegateNoLambda();
            }
        }

        MessageDescriptor GetDescriptor(Type type)
        {
            PropertyInfo propertyInfo = type.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static);
            if (propertyInfo == null)
                throw new InvalidOperationException("Argument message type doesn't have Descriptor property");
            return propertyInfo.GetValue(null) as MessageDescriptor;
        }
        
        void CreateTaskResultDelegateNoLambda()
        {
            var prop = _returnType.GetProperty("Result");
            if (prop == null)
                throw new InvalidOperationException("Return value doesn't have Result property");
            _getTaskResultDelegate = task => prop.GetValue(task) as IMessage;
        }

        void CreateTaskResultDelegate()
        {
            Type taskType = _returnType;
            var prop = taskType.GetProperty("Result");
            if (prop == null)
                throw new InvalidOperationException("Return value doesn't have Result property");
            
            MethodInfo methodInfo = prop.GetGetMethod();
            ParameterExpression param0 = Expression.Parameter(typeof(Task), "task");

            MethodCallExpression body = Expression.Call(
                Expression.Convert(param0, taskType),
                methodInfo);

            Expression newBody = Expression.Convert(body, typeof(IMessage));

            _getTaskResultDelegate = Expression.Lambda<DGetTaskResult>(newBody, param0).Compile();
        }

        void CreateDelegateNoLambda()
        {
            _invokeDelegate = (entity, obj, cancellationToken) =>
            {
                object[] args = new object[_parameters.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    if (i == argumentIndex)
                        args[i] = obj;
                    if (i == cancellationTokenIndex)
                        args[i] = cancellationToken;
                }
                
                return _methodInfo.Invoke(entity, args);
            };
        }

        void CreateDelegate()
        {
            ParameterExpression entityParam = Expression.Parameter(typeof(object), "instance");
            ParameterExpression argParam = Expression.Parameter(typeof(IMessage), "arg");
            ParameterExpression cancellationTokenParam =
                Expression.Parameter(typeof(CancellationToken), "cancellationToken");

            Expression[] methodArguments = new Expression[_parameters.Length];
            for (int i = 0; i < methodArguments.Length; i++)
            {
                Type parameterType = _parameters[i].ParameterType;
                if (i == argumentIndex)
                    methodArguments[i] = parameterType.GetTypeInfo().IsValueType
                        ? Expression.Unbox(argParam, parameterType)
                        : Expression.Convert(argParam, parameterType);
                if (i == cancellationTokenIndex)
                    methodArguments[i] = cancellationTokenParam;
            }

            var callBody = Expression.Call(
                Expression.Convert(entityParam, _declaringType),
                _methodInfo,
                methodArguments);

            
            Expression resultBody;
            if (_isVoidMethod)
            {
                resultBody = Expression.Block(callBody, Expression.Constant(null));
            }
            else
            {
                resultBody = Expression.Convert(callBody, typeof(object));
            }

            _invokeDelegate = Expression.Lambda<DInvokeDelegate>(resultBody, entityParam, argParam, cancellationTokenParam).Compile();
        }

        /// <summary>
        /// Invoke async method
        /// </summary>
        /// <param name="entity">Instance of entity</param>
        /// <param name="argument">Method argument</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task that represents result of method invocation</returns>
        public async Task<IMessage> InvokeAsync(object entity, IMessage argument, CancellationToken cancellationToken)
        {
            object result = _invokeDelegate(entity, argument, cancellationToken);
            if (_isVoidMethod)
                return null;
            if (_doesReturnTask)
            {
                Task t = (Task)result;
                await t.ConfigureAwait(false);

                if (_doesReturnValue)
                    return _getTaskResultDelegate(t);
                return null;
            }

            return (IMessage)result;
        }

    }
}
