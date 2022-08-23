namespace Neon.Rpc.Net
{
    public interface ISessionFactory
    {
        RpcSession CreateSession(RpcSessionContext sessionContext);
    }

    public interface IAuthenticatedSessionFactory
    {
        void Authenticate(object data);
    }
}
