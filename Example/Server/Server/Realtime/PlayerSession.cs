using Neon.Rpc;
using Neon.Rpc.Events;
using Neon.Rpc.Payload;
using Neon.ServerExample.Proto;
using Neon.ServerExample.Realtime.Rooms;

namespace Neon.ServerExample.Realtime;

// Realtime player session
public class PlayerSession : RpcSessionImpl
{
    readonly RoomController roomController;

    Room? room;
    Player? player;
    
    public PlayerSession(RoomController roomController, RpcSessionContext sessionContext) : base(sessionContext)
    {
        this.roomController = roomController;
    }

    void CheckJoined()
    {
        if (room == null || player == null)
            throw new RemotingException("You are not joined to any room", RemotingException.StatusCodeEnum.Internal);
    }

    //OnClose we must leave the room (if joined)
    protected override void OnClose(OnCloseEventArgs args)
    {
        if (room != null && player != null)
        {
            room!.Leave(player!);
            room = null;
            player = null;
        }
    }

    //Join room request
    [RemotingMethod]
    int JoinRoom(RealtimeRoomProto roomInfo)
    {
        if (!this.roomController.TryGetRoom(roomInfo.RoomGuid.ToGuid(), out Room? room_))
            throw new RemotingException("Room not found", RemotingException.StatusCodeEnum.Internal);

        this.room = room_;
        this.player = room_.Join(this);

        return this.player.Id;
    }

    //Leave room request
    [RemotingMethod]
    void LeaveRoom()
    {
        CheckJoined();
        room!.Leave(player!);
        room = null;
        player = null;
    }
    
    //Adds a new bot to the room
    [RemotingMethod]
    void AddBot()
    {
        CheckJoined();
        room!.AddBot();
    }
    
    //Removes first AI player found in the room or throws an exception
    [RemotingMethod]
    void RemoveBot()
    {
        CheckJoined();
        room!.RemoveBot();
    }

    //Player move request, several move requests in one tick cause only the latest will be processed
    [RemotingMethod]
    void Move(ClientMovedMessageProto movedMessage)
    {
        CheckJoined();
        player!.ApplyMoveVector(new Vector2(movedMessage.X, movedMessage.Y));
    }
}