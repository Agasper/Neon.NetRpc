using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Neon.Rpc.Payload;

namespace Neon.Rpc
{
    public class MethodContainer
    {
        delegate object InvokeDelegate(object session, params object[] obj);

        delegate object DTaskResultDelegate(Task task);
        static readonly ConcurrentDictionary<Type, DTaskResultDelegate> taskResultsCache = new ConcurrentDictionary<Type, DTaskResultDelegate>();

        public bool IsAsyncMethod { get; private set; }
        public bool DoesReturnValue { get; private set; }
        public bool IsVoidMethod { get; private set; }
        public MethodInfo MethodInfo { get; private set; }
        public ParameterInfo[] Parameters { get; private set; }
        public Type ReturnType { get; private set; }

        public bool CanUseLambda { get; private set; }

        InvokeDelegate invokeDelegate;

        public MethodContainer(MethodInfo methodInfo, bool canUseLambda)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            this.MethodInfo = methodInfo;
            this.CanUseLambda = canUseLambda;

            this.Parameters = methodInfo.GetParameters();

            IsAsyncMethod = methodInfo.ReturnType.IsGenericType &&
                methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
            IsAsyncMethod |= methodInfo.ReturnType == typeof(Task);

            DoesReturnValue = methodInfo.ReturnType != typeof(void) &&
                methodInfo.ReturnType != typeof(Task);
            IsVoidMethod = methodInfo.ReturnType == typeof(void);

            ReturnType = methodInfo.ReturnType;

            if (canUseLambda)
                CreateDelegate();
            else
                CreateDelegateNoLambda();
        }

        void CreateDelegateNoLambda()
        {
            if (IsVoidMethod)
            {
                invokeDelegate = (session, obj) => MethodInfo.Invoke(session, obj);
            }
            else
            {
                invokeDelegate = (session, obj) => MethodInfo.Invoke(session, obj);
            }
        }

        void CreateDelegate()
        {
            ParameterExpression entityParam = Expression.Parameter(typeof(object), "entity");
            ParameterExpression arrayObjectParam = Expression.Parameter(typeof(object[]), "args");

            MethodCallExpression callBody;

            if (Parameters.Length > 0)
            {
                Expression[] methodArguments = new Expression[Parameters.Length];
                for (int i = 0; i < methodArguments.Length; i++)
                {
                    Type parameterType = Parameters[i].ParameterType;
                    Expression argument = Expression.ArrayAccess(arrayObjectParam, Expression.Constant(i));
                    argument = parameterType.GetTypeInfo().IsValueType ? Expression.Unbox(argument, parameterType) : Expression.Convert(argument, parameterType);
                    methodArguments[i] = argument;
                }

                callBody = Expression.Call(
                    Expression.Convert(entityParam, MethodInfo.DeclaringType),
                    MethodInfo,
                    methodArguments);
            }
            else
            {
                callBody = Expression.Call(
                    Expression.Convert(entityParam, MethodInfo.DeclaringType),
                    MethodInfo);
            }


            Expression resultBody;
            var excepParam = Expression.Parameter(typeof(Exception));
            if (IsVoidMethod)
            {
                Expression funcBody = Expression.Block(callBody, Expression.Constant(null));

                resultBody = funcBody;// Expression.TryCatch(funcBody, Expression.Catch(excepParam, Expression.Throw(Expression.New(InvokationException.GetConstructorInfo(), excepParam), typeof(object))));
            }
            else
            {
                Expression funcBody = callBody;
                if (ReturnType.GetTypeInfo().IsValueType)
                    funcBody = Expression.Convert(funcBody, typeof(object));

                resultBody = funcBody;// Expression.TryCatch(funcBody, Expression.Catch(excepParam, Expression.Throw(Expression.New(InvokationException.GetConstructorInfo(), excepParam), funcBody.Type)));
            }

            invokeDelegate = Expression.Lambda<InvokeDelegate>(resultBody, entityParam, arrayObjectParam).Compile();
        }

        object InvokeInternal(object entity, params object[] arguments)
        {
            if (arguments.Length != Parameters.Length)
                throw new RemotingException($"Invalid method {MethodInfo.DeclaringType.Name}.{MethodInfo.Name} parameters count", RemotingException.StatusCodeEnum.MethodSignatureMismatch);

            //try
            //{
            return invokeDelegate(entity, arguments);
            //}
            //catch(InvokationException iEx)
            //{
            //    throw RemoteException.CreateFromException(iEx.InnerException, $"Method invokation {MethodInfo.DeclaringType.Name}.{MethodInfo.Name} exception. " + iEx.InnerException.Message);
            //}
        }

        public object Invoke(object entity, params object[] arguments)
        {
            if (IsAsyncMethod)
                throw new InvalidOperationException("Async methods can be executed only by InvokeAsync method");

            return InvokeInternal(entity, arguments);
        }

        public async Task<object> InvokeAsync(object entity, params object[] arguments)
        {
            if (!IsAsyncMethod)
                return Invoke(entity, arguments);

            Task task = InvokeInternal(entity, arguments) as Task;

            // try
            // {
            await task.ConfigureAwait(false);
            // }
            //catch (InvokationException iEx)
            //{
            //    throw RemoteException.CreateFromException(iEx.InnerException, $"Method invokation {MethodInfo.DeclaringType.Name}.{MethodInfo.Name} exception. " + iEx.InnerException.Message);
            //}
            // catch (Exception ex)
            // {
            //     throw new RemotingException($"Method invokation {MethodInfo.DeclaringType.Name}.{MethodInfo.Name} exception.", ex);
            // }

            if (!DoesReturnValue)
                return null;

            if (CanUseLambda)
                return GetTaskResult(task);
            else
                return task.GetType().GetProperty("Result").GetValue(task);
        }

        object GetTaskResult(Task task)
        {
            Type taskType = task.GetType();
            if (taskResultsCache.TryGetValue(taskType, out DTaskResultDelegate taskResultDelegate))
            {
                return taskResultDelegate(task);
            }
            else
            {
                MethodInfo methodInfo = taskType.GetProperty("Result").GetGetMethod();

                ParameterExpression param0 = Expression.Parameter(typeof(object), "task");

                MethodCallExpression body = Expression.Call(
                    Expression.Convert(param0, taskType),
                    methodInfo);

                Expression newBody = body;
                Type resultType = methodInfo.ReturnType;
                if (resultType.GetTypeInfo().IsValueType)
                    newBody = Expression.Convert(body, typeof(object));

                taskResultDelegate = Expression.Lambda<DTaskResultDelegate>(newBody, param0).Compile();
                taskResultsCache.TryAdd(taskType, taskResultDelegate);
                return taskResultDelegate(task);
            }
        }
    }
}
