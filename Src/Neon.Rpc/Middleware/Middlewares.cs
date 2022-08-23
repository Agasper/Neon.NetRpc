using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neon.Networking.Messages;

namespace Middleware
{
    public class Middlewares
    {
        List<IMiddleware> middlewares;
        volatile int startIndex;
        bool starting;
        
        public Middlewares()
        {
            this.middlewares = new List<IMiddleware>();
        }

        public void Add(IMiddleware middleware)
        {
            this.middlewares.Add(middleware);
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            starting = true;
            for (startIndex = 0; startIndex < this.middlewares.Count; Interlocked.Increment(ref startIndex))
            {
                CheckToken(cancellationToken);
                await this.middlewares[startIndex].Start(cancellationToken).ConfigureAwait(false);
            }
        }
        
        void CheckToken(CancellationToken token)
        {
            if (token.IsCancellationRequested)
                throw new OperationCanceledException("Connection was closed prematurely");
        }

        public RawMessage ProcessRecvMessage(RawMessage rawMessage)
        {
            if (!starting)
                throw new InvalidOperationException("Middlewares not started. Please call Start()");
            if (rawMessage == null) 
                throw new ArgumentNullException(nameof(rawMessage));
            if (middlewares.Count == 0)
                return rawMessage;

            RawMessage workingMessage = rawMessage;

            for (int i = startIndex - 1; i >= 0 ; i--)
            {
                workingMessage = middlewares[i].ReceiveMessage(workingMessage);
                if (workingMessage == null)
                    break;
            }

            if (startIndex < this.middlewares.Count)
            {
                this.middlewares[startIndex].MiddlewareMessage(workingMessage);
                workingMessage.Dispose();
                return null;
            }

            return workingMessage;
        }

        public RawMessage ProcessSendMessage(RawMessage rawMessage)
        {
            if (!starting)
                throw new InvalidOperationException("Middlewares not started. Please call Start()");
            if (rawMessage == null) 
                throw new ArgumentNullException(nameof(rawMessage));
            if (middlewares.Count == 0)
                return rawMessage;

            RawMessage workingMessage = rawMessage;
            int count = startIndex;
            if (count >= this.middlewares.Count)
                count -= 1;
                
            for (int i = 0; i <= count; i++)
            {
                workingMessage = middlewares[i].SendMessage(workingMessage);
                if (workingMessage == null)
                    break;
            }

            return workingMessage;
        }

    }
}