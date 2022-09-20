using Neon.Rpc.Authorization;
using Neon.Rpc.Payload;
using Neon.ServerExample.Proto;
using Niarru.Zodchy.Server.Data;

namespace Neon.ServerExample.Backend;

public class AuthSession : AuthSessionServerAsync
{
    public PlayerProfileProto? PlayerProfile { get; private set; }
    public PlayerCredentials Credentials { get; private set; }
    
    readonly IDataStore<PlayerProfileProto> dataStore;
    
    public AuthSession(IDataStore<PlayerProfileProto> dataStore, AuthSessionContext sessionContext) : base(sessionContext)
    {
        this.dataStore = dataStore;
    }

    //Player auth method
    protected override async Task<object?> Auth(object? arg)
    {
        switch (arg)
        {
            //if client have credentials, we'll try to load a player and return null
            case LoginCredentialsProto loginCredentialsProto:
                this.Credentials = new PlayerCredentials(loginCredentialsProto.Id, loginCredentialsProto.Token);
                this.PlayerProfile =
                    await dataStore.Load(new PlayerCredentials(loginCredentialsProto.Id, loginCredentialsProto.Token));
                return null;
            //if client doesn't have credentials we should create a new player and return credentials
            case null:
                this.PlayerProfile = new PlayerProfileProto();
                this.Credentials = await dataStore.Create(this.PlayerProfile);
                return new LoginCredentialsProto() {Id = this.Credentials.Id, Token = this.Credentials.Token};
            default:
                throw new RemotingException($"Wrong auth argument",
                    RemotingException.StatusCodeEnum.UserDefined);
        }
    }
}