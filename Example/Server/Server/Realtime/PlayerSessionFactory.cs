using Neon.Rpc;
using Neon.Rpc.Net;
using Neon.ServerExample.Realtime.Rooms;

namespace Neon.ServerExample.Realtime;

public class PlayerSessionFactory : ISessionFactory
{
    readonly RoomController roomController;

    public PlayerSessionFactory(RoomController roomController)
    {
        this.roomController = roomController;
    }
    
    public RpcSession CreateSession(RpcSessionContext sessionContext)
    {
        return new PlayerSession(roomController, sessionContext);
    }
}