using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Neon.Rpc.Payload;

namespace Neon.Rpc
{
    
    /// <summary>
    /// Container optimizes any method execution with a lambda expressions
    /// </summary>
    public class MethodContainer
    {
        delegate object DInvokeDelegate(object session, params object[] obj);
        delegate object DGetTaskResult(Task task);

        /// <summary>
        /// Does this method return Task or Task`
        /// </summary>
        public bool DoesReturnTask { get;  }
        
        /// <summary>
        /// false if this method void or return Task, otherwise true
        /// </summary>
        public bool DoesReturnValue { get;  }
        
        /// <summary>
        /// If this method void
        /// </summary>
        public bool IsVoidMethod { get;  }
        
        /// <summary>
        /// Method info
        /// </summary>
        public MethodInfo MethodInfo { get; }
        
        /// <summary>
        /// Method arguments
        /// </summary>
        public ParameterInfo[] Parameters { get; }
        
        /// <summary>
        /// Method return type
        /// </summary>
        public Type ReturnType { get; }

        /// <summary>
        /// Can we use lambda functions for invocation optimizations
        /// </summary>
        public bool CanUseLambda { get; }

        DGetTaskResult getTaskResultDelegate;
        DInvokeDelegate invokeDelegate;

        public MethodContainer(MethodInfo methodInfo, bool canUseLambda)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            this.MethodInfo = methodInfo;
            this.CanUseLambda = canUseLambda;

            this.Parameters = methodInfo.GetParameters();

            bool doesReturnGenericTask = methodInfo.ReturnType.IsGenericType &&
                                         methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
            
            DoesReturnTask = doesReturnGenericTask || methodInfo.ReturnType == typeof(Task);

            DoesReturnValue = methodInfo.ReturnType != typeof(void) &&
                methodInfo.ReturnType != typeof(Task);
            IsVoidMethod = methodInfo.ReturnType == typeof(void);

            ReturnType = methodInfo.ReturnType;

            if (canUseLambda)
                CreateDelegate();
            else
                CreateDelegateNoLambda();

            if (DoesReturnTask && DoesReturnValue)
            {
                if (canUseLambda)
                    CreateTaskResultDelegate();
                else
                    CreateTaskResultDelegateNoLambda();
            }
        }
        
        void CreateTaskResultDelegateNoLambda()
        {
            PropertyInfo resultGetProperty = ReturnType.GetProperty("Result");
            getTaskResultDelegate = task => resultGetProperty.GetValue(task);
        }

        void CreateTaskResultDelegate()
        {
            Type taskType = ReturnType;
            
            MethodInfo methodInfo = taskType.GetProperty("Result").GetGetMethod();

            ParameterExpression param0 = Expression.Parameter(typeof(Task), "task");

            MethodCallExpression body = Expression.Call(
                Expression.Convert(param0, taskType),
                methodInfo);

            Expression newBody = body;
            Type resultType = methodInfo.ReturnType;
            if (resultType.GetTypeInfo().IsValueType)
                newBody = Expression.Convert(body, typeof(object));

            getTaskResultDelegate = Expression.Lambda<DGetTaskResult>(newBody, param0).Compile();
        }

        void CreateDelegateNoLambda()
        {
            if (IsVoidMethod)
            {
                invokeDelegate = (entity, obj) => MethodInfo.Invoke(entity, obj);
            }
            else
            {
                invokeDelegate = (entity, obj) => MethodInfo.Invoke(entity, obj);
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
            // var exceptionParam = Expression.Parameter(typeof(Exception));
            if (IsVoidMethod)
            {
                Expression funcBody = Expression.Block(callBody, Expression.Constant(null));

                resultBody = funcBody;// Expression.TryCatch(funcBody, Expression.Catch(exceptionParam, Expression.Throw(Expression.New(InvokationException.GetConstructorInfo(), excepParam), typeof(object))));
            }
            else
            {
                Expression funcBody = callBody;
                if (ReturnType.GetTypeInfo().IsValueType)
                    funcBody = Expression.Convert(funcBody, typeof(object));

                resultBody = funcBody;// Expression.TryCatch(funcBody, Expression.Catch(exceptionParam, Expression.Throw(Expression.New(InvokationException.GetConstructorInfo(), excepParam), funcBody.Type)));
            }

            invokeDelegate = Expression.Lambda<DInvokeDelegate>(resultBody, entityParam, arrayObjectParam).Compile();
        }

        object InvokeInternal(object entity, params object[] arguments)
        {
            // if (arguments.Length != Parameters.Length)
            //     throw new RemotingException($"Invalid method {MethodInfo.DeclaringType.Name}.{MethodInfo.Name} parameters count", RemotingException.StatusCodeEnum.MethodSignatureMismatch);

            //try
            //{
            return invokeDelegate(entity, arguments);
            //}
            //catch(InvokationException iEx)
            //{
            //    throw RemoteException.CreateFromException(iEx.InnerException, $"Method invokation {MethodInfo.DeclaringType.Name}.{MethodInfo.Name} exception. " + iEx.InnerException.Message);
            //}
        }

        /// <summary>
        /// Invoke non-async method
        /// </summary>
        /// <param name="entity">Instance of entity</param>
        /// <param name="arguments">Method arguments</param>
        /// <returns>Result of method invocation</returns>
        /// <exception cref="InvalidOperationException">If method is async</exception>
        public object Invoke(object entity, params object[] arguments)
        {
            if (DoesReturnTask)
                throw new InvalidOperationException("Async methods can be executed only by InvokeAsync method");

            return InvokeInternal(entity, arguments);
        }

        /// <summary>
        /// Invoke async method
        /// </summary>
        /// <param name="entity">Instance of entity</param>
        /// <param name="arguments">Method arguments</param>
        /// <returns>A task that represents result of method invocation</returns>
        public async Task<object> InvokeAsync(object entity, params object[] arguments)
        {
            if (!DoesReturnTask)
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

            return getTaskResultDelegate(task);
        }

    }
}
