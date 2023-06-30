using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Neon.Rpc.Messages.Proto;

namespace Neon.Rpc.Messages
{
    public class RpcException : Exception
    {
        public RpcResponseStatusCode StatusCode { get; }
        
        /// <summary>
        /// Gets a string representation of the immediate frames on the call stack 
        /// </summary>
        public override string StackTrace
        {
            get
            {
                if (string.IsNullOrEmpty(remoteStackTrace))
                    return base.StackTrace;
                return remoteStackTrace;
            }
        }

        public override IDictionary Data => meta;
        public IDictionary<string, string> Meta => meta;

        readonly string remoteStackTrace;
        readonly Dictionary<string, string> meta;

        public RpcException(string message) : base(message)
        {
            meta = new Dictionary<string, string>();
            StatusCode = RpcResponseStatusCode.Internal;
        }
        
        public RpcException(string message, RpcResponseStatusCode statusCode) : this(message)
        {
            StatusCode = statusCode;
        }
        
        internal RpcException(Exception exception, RpcResponseStatusCode statusCode) : this(exception.Message)
        {
            remoteStackTrace = exception.StackTrace;
            StatusCode = statusCode;
        }
        
        internal RpcException(Exception exception) : this(exception, RpcResponseStatusCode.Internal)
        {
        }

        internal RpcException(RpcExceptionProto proto, RpcResponseStatusCode statusCode) : this(proto.Message)
        {
            remoteStackTrace = proto.StackTrace;
            StatusCode = statusCode;
            foreach (var pair in proto.Meta)
            {
                meta[pair.Key] = pair.Value;
            }
        }

        internal RpcExceptionProto CreateProto()
        {
            RpcExceptionProto result = new RpcExceptionProto();
            result.Message = Message;
            result.StackTrace = StackTrace;
            foreach (var pair in meta)
            {
                result.Meta.Add(pair.Key, pair.Value);
            }

            return result;
        }

        public static RpcException ConvertException(Exception exception)
        {
            if (exception is RpcException rpcException)
                return rpcException;
            else if (exception is NotSupportedException)
                return new RpcException(exception, RpcResponseStatusCode.NotSupported);
            else if (exception is NotImplementedException)
                return new RpcException(exception, RpcResponseStatusCode.NotImplemented);
            else if (exception is InvalidOperationException)
                return new RpcException(exception, RpcResponseStatusCode.InvalidOperation);
            else if (exception is ArgumentException)
                return new RpcException(exception, RpcResponseStatusCode.InvalidArgument);
            else if (exception is OperationCanceledException)
                return new RpcException(exception, RpcResponseStatusCode.Cancelled);
            else if (exception is TimeoutException)
                return new RpcException(exception, RpcResponseStatusCode.TimeoutExceeded);
            else if (exception is UnauthorizedAccessException)
                return new RpcException(exception, RpcResponseStatusCode.Unauthenticated);
            return new RpcException(exception, RpcResponseStatusCode.Internal);
        }
        
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendFormat("{0}({1})", GetType().Name, StatusCode.ToString());
            
            if (!string.IsNullOrEmpty(Message))
                sb.Append(": " + Message);

            if (!string.IsNullOrEmpty(StackTrace))
            {
                sb.Append(Environment.NewLine + StackTrace);
            }

            return sb.ToString();
        }
    }
}