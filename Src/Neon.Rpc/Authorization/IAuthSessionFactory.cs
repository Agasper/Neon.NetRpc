using System.Threading.Tasks;

namespace Neon.Rpc.Authorization
{
    /// <summary>
    /// A factory for server authentication session
    /// </summary>
    public interface IAuthSessionFactory
    {
        /// <summary>
        /// Method must return a new instance of server authentication session (AuthSessionServer or AuthSessionServerAsync)
        /// </summary>
        /// <param name="sessionContext">Provided context to pass to the session</param>
        /// <returns>Instance of the new server authentication session</returns>
        AuthSessionServerBase CreateSession(AuthSessionContext sessionContext);
    }
}