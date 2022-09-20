using Neon.Rpc.Authorization;
using Neon.ServerExample.Proto;
using Niarru.Zodchy.Server.Data;

namespace Neon.ServerExample.Backend;

public class AuthSessionFactory : IAuthSessionFactory
{
    readonly IDataStore<PlayerProfileProto> dataStore;
    
    public AuthSessionFactory(IDataStore<PlayerProfileProto> dataStore)
    {
        this.dataStore = dataStore;
    }
    
    public AuthSessionServerBase CreateSession(AuthSessionContext sessionContext)
    {
        return new AuthSession(dataStore, sessionContext);
    }
}