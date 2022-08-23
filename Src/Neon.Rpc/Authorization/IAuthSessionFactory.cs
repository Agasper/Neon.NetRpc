using System.Threading.Tasks;

namespace Neon.Rpc.Authorization
{
    public interface IAuthSessionFactory
    {
        AuthSessionServerBase CreateSession(AuthSessionContext sessionContext);
    }
}