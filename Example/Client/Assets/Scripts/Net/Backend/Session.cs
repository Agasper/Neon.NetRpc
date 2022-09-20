using System;
using System.Threading.Tasks;
using Neon.Rpc;
using Neon.Rpc.Authorization;
using Neon.ServerExample.Proto;

namespace Neon.ClientExample.Net.Backend
{
    public class Session : RpcSessionImpl
    {
        public LoginCredentialsProto NewCredentials => newCredentials;
        public PlayerProfileModel Model => model;
        
        LoginCredentialsProto newCredentials;
        PlayerProfileModel model;
        
        public Session(RpcSessionContext sessionContext) : base(sessionContext)
        {
            AuthSessionClient authSessionClient = sessionContext.AuthSession as AuthSessionClient;
            if (authSessionClient == null)
                throw new InvalidOperationException("Auth session is null");
            //Checks auth session for new credentials. If it's an old account server returns null
            this.newCredentials = authSessionClient.AuthResult as LoginCredentialsProto;
        }

        //getting profile from the server and creates model
        public async Task CreateModel()
        {
            PlayerProfileProto profile = await this.ExecuteAsync<PlayerProfileProto>("GetProfile");
            this.model = new PlayerProfileModel(this, profile);
        }
    }
}