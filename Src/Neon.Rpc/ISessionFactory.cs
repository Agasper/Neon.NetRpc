﻿using System.Threading;
using System.Threading.Tasks;

namespace Neon.Rpc
{
    /// <summary>
    /// RPC session factory
    /// </summary>
    public interface ISessionFactory
    {
        Task AuthenticateAsync(AuthenticationContext context, CancellationToken cancellationToken);
        Task<RpcSessionBase> CreateSessionAsync(RpcSessionContext context, CancellationToken cancellationToken);
    }
}