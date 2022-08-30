using System;
using System.Runtime.Serialization;
using System.Text;
using Neon.Networking.IO;
using Neon.Networking.Messages;
using Neon.Rpc.Serialization;

namespace Neon.Rpc.Payload
{
    public class RemotingException : Exception
    {
        public enum StatusCodeEnum : byte
        {
            Internal = 0,
            AccessDenied = 1,
            MethodSignatureMismatch = 2,
            QueueExceeded = 3,
            ConnectionIssues = 4,
            Timeout = 5,
            UserDefined = 6
        }
        
        /// <summary>
        /// Gets a string representation of the immediate frames on the call stack 
        /// </summary>
        public override string StackTrace
        {
            get
            {
                if (string.IsNullOrEmpty(remoteStackTrace))
                    return base.StackTrace;
                else
                    return remoteStackTrace;
            }
        }
        /// <summary>
        /// Exception status code
        /// </summary>
        public StatusCodeEnum StatusCode => statusCode;
        /// <summary>
        /// If this exception is caused by another one return a type of inner exception
        /// </summary>
        public string InnerExceptionType => remoteType;

        string remoteStackTrace;
        string remoteType;
        StatusCodeEnum statusCode;

        public RemotingException(string message, StatusCodeEnum statusCode) : base(message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            this.remoteType = "";
            this.remoteStackTrace = base.StackTrace;
            this.statusCode = statusCode;
        }

        internal RemotingException(Exception exception) : base(exception.Message, exception)
        {
            this.remoteStackTrace = exception.StackTrace;
            this.remoteType = exception.GetType().FullName;
            this.statusCode = StatusCodeEnum.Internal;
        }

        internal RemotingException(IRpcMessage message) : base(message.ReadString())
        {
            remoteStackTrace = message.ReadString();
            remoteType = message.ReadString();
            statusCode = (StatusCodeEnum) message.ReadByte();
        }

        internal virtual void WriteTo(IRpcMessage message)
        {
            message.Write(this.Message);
            message.Write(this.StackTrace == null ? string.Empty : this.StackTrace);
            message.Write(this.remoteType);
            message.Write((byte)this.statusCode);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (string.IsNullOrEmpty(remoteType) || remoteType == nameof(RemotingException))
                sb.AppendFormat("{0}({1})", this.GetType().Name, statusCode.ToString());
            else
                sb.AppendFormat("{0}({1}) from {2}", this.GetType().Name, statusCode.ToString(), remoteType);

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
