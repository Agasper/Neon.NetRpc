using System;
using Neon.Rpc.Net;

namespace Neon.Rpc
{
    public delegate RpcSession DCreateSession(RpcSessionContext context);

    /// <summary>
    /// Session factory helper class to create sessions from lambda
    /// </summary>
    public class InlineSessionFactory : ISessionFactory
    {
        DCreateSession generator;

        public InlineSessionFactory(DCreateSession generator)
        {
            if (generator == null)
                throw new ArgumentNullException(nameof(generator));
            this.generator = generator;
        }

        public RpcSession CreateSession(RpcSessionContext sessionContext)
        {
            return generator(sessionContext);
        }
    }
}