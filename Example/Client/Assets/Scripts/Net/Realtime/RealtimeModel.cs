using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Neon.ClientExample.Net.Util;
using Neon.ServerExample.Proto;

namespace Neon.ClientExample.Net.Realtime
{
    //Model represents state of the realtime connection
    public class RealtimeModel
    {
        //Returns room model if we joined the room or null
        public RoomModel RoomModel => roomModel;
        //Event raised when we join to a room
        public IGameEvent<RoomModel> OnRoomJoined => onRoomJoined;
        //Event raised when we left a room
        public IGameEvent OnRoomLeft => onRoomLeft;

        GameEvent<RoomModel> onRoomJoined;
        GameEvent onRoomLeft;
        
        readonly Session session;

        RoomModel roomModel;
        
        public RealtimeModel([NotNull] Session session)
        {
            this.onRoomJoined = new GameEvent<RoomModel>();
            this.onRoomLeft = new GameEvent();
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }
        
        //Join room request
        public async Task JoinRoom(RealtimeRoomProto roomProto)
        {
            int myId  = await session.ExecuteAsync<int, RealtimeRoomProto>("JoinRoom", roomProto);
            roomModel = new RoomModel(session, myId);
            onRoomJoined.Invoke(roomModel);
        }
        
        //Leave room request
        public async Task LeaveRoom()
        {
            await session.ExecuteAsync("LeaveRoom");
            roomModel = null;
            onRoomLeft.Invoke();
        }

        //Updating state of the room if we joined
        internal void UpdateState(RoomStateProto roomStateProto)
        {
            roomModel?.UpdateState(roomStateProto);
        }
    }
}