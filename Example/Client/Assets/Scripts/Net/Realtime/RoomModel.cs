using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Neon.ClientExample.Net.Util;
using Neon.Networking.Udp.Messages;
using Neon.Rpc;
using Neon.ServerExample.Proto;

namespace Neon.ClientExample.Net.Realtime
{
    //Class represents current room state
    public class RoomModel
    {
        //Your player id in the room
        public int MyId { get; }
        //Event raised when we get room state update
        public IGameEvent<RoomStateProto> OnRoomStateUpdate => onRoomStateUpdate;
        
        GameEvent<RoomStateProto> onRoomStateUpdate;

        readonly Session session;
        
        public RoomModel([NotNull] Session session, int myId)
        {
            this.MyId = myId;
            this.onRoomStateUpdate = new GameEvent<RoomStateProto>();
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        //raise an event
        internal void UpdateState(RoomStateProto roomStateProto)
        {
            onRoomStateUpdate.Invoke(roomStateProto);
        }
        
        //Ask server to add one bot to the room
        public Task AddBot()
        {
            return session.ExecuteAsync("AddBot");
        }
        
        //Ask server to remove one bot from the room
        public Task RemoveBot()
        {
            return session.ExecuteAsync("RemoveBot");
        }

        //Send the current player's move vector
        public Task Move(ClientMovedMessageProto movedMessage)
        {
            return session.Send<ClientMovedMessageProto>("Move", movedMessage, SendingOptions.Default.WithDeliveryType(DeliveryType.UnreliableSequenced));
        }
    }
}