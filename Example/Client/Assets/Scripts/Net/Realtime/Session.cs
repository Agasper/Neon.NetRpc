using System;
using System.Threading.Tasks;
using Neon.Rpc;
using Neon.Rpc.Authorization;
using Neon.ServerExample.Proto;

namespace Neon.ClientExample.Net.Realtime
{
    public class Session : RpcSessionImpl
    {
        public RealtimeModel Model => realtimeModel;
        
        RealtimeModel realtimeModel;
        
        public Session(RpcSessionContext sessionContext) : base(sessionContext)
        {
            realtimeModel = new RealtimeModel(this);
        }

        //Received room state updates
        [RemotingMethod]
        void RoomStateUpdate(RoomStateProto roomStateProto)
        {
            realtimeModel?.UpdateState(roomStateProto);
        }
    }
}