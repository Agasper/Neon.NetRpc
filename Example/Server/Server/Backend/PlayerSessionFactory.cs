using Neon.Rpc;
using Neon.Rpc.Net;
using Neon.ServerExample.Proto;
using Neon.ServerExample.Realtime.Rooms;
using Niarru.Zodchy.Server.Data;

namespace Neon.ServerExample.Backend;

public class PlayerSessionFactory : ISessionFactory
{
    readonly IDataStore<PlayerProfileProto> dataStore;
    readonly RoomController roomController;

    public PlayerSessionFactory(IDataStore<PlayerProfileProto> dataStore, RoomController roomController)
    {
        this.roomController = roomController ?? throw new ArgumentNullException(nameof(roomController));
        this.dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
    }

    public RpcSession CreateSession(RpcSessionContext sessionContext)
    {
        return new PlayerSession(dataStore, roomController, sessionContext);
    }
}