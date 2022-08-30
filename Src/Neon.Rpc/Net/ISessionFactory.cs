namespace Neon.Rpc.Net
{
    /// <summary>
    /// RPC session factory
    /// </summary>
    public interface ISessionFactory
    {
        RpcSession CreateSession(RpcSessionContext sessionContext);
    }

    /// <summary>
    /// Authentication session factory
    /// </summary>
    public interface IAuthenticatedSessionFactory
    {
        void Authenticate(object data);
    }
}
