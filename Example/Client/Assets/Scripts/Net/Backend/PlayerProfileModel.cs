using System;
using System.Threading.Tasks;
using Neon.ClientExample.Net.Util;
using Neon.ServerExample.Proto;

namespace Neon.ClientExample.Net.Backend
{
    //Player profile model reflects current profile state on the server
    public class PlayerProfileModel
    {
        //You can subscribe to the money property
        //if it will be changed on the server, you receive update
        public IReadOnlyModelProperty<int> Money => money;

        readonly ModelProperty<int> money;
        readonly Session session;

        public PlayerProfileModel(Session session)
        {
            this.session = session;
            this.money = new ModelProperty<int>();
        }
        
        public PlayerProfileModel(Session session, PlayerProfileProto playerProfile) : this(session)
        {
            money.Value = playerProfile.Money;
        }

        public async Task<int> AddMoney()
        {
            //Ask server to add money and return added amount
            int addedAmount = await session.ExecuteAsync<int>("AddMoney");
            //updating property, so views could update their state
            this.money.Value += addedAmount;
            return addedAmount;
        }
        
        //Getting rooms from the server
        public Task<RealtimeRoomCollectionProto> GetRooms()
        {
            return session.ExecuteAsync<RealtimeRoomCollectionProto>("GetRooms");
        }
        
        //Creates new room on the server
        public Task<RealtimeRoomProto> CreateRoom()
        {
            return session.ExecuteAsync<RealtimeRoomProto>("CreateRoom");
        }
    }
}