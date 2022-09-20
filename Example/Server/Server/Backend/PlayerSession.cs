using Neon.Rpc;
using Neon.Rpc.Events;
using Neon.ServerExample.Proto;
using Neon.ServerExample.Realtime.Rooms;
using Niarru.Zodchy.Server.Data;

namespace Neon.ServerExample.Backend;

public class PlayerSession : RpcSessionImpl
{
    readonly IDataStore<PlayerProfileProto> dataStore;
    readonly RoomController roomController;
    readonly PlayerProfileProto profile;
    readonly PlayerCredentials credentials;
    readonly Random random;
    
    public PlayerSession(IDataStore<PlayerProfileProto> dataStore, RoomController roomController, RpcSessionContext sessionContext) : base(sessionContext)
    {
        this.dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        this.roomController = roomController ?? throw new ArgumentNullException(nameof(roomController));
        
        //getting player credentials & profile from the auth session
        AuthSession authSession = sessionContext.AuthSession as AuthSession ?? throw new InvalidOperationException("Auth session is null");
        this.profile = authSession.PlayerProfile ?? throw new InvalidOperationException("Player profile not set");
        this.credentials = authSession.Credentials;
        this.random = new Random();
    }

    //return player profile to the client
    [RemotingMethod]
    PlayerProfileProto GetProfile()
    {
        return profile;
    }

    //adding random amount of money to the profile
    [RemotingMethod]
    int AddMoney()
    {
        int amount = random.Next(1, 100);
        profile.Money += amount;
        return amount;
    }

    //returning all the available rooms
    [RemotingMethod]
    RealtimeRoomCollectionProto GetRooms()
    {
        return roomController.GetRoomsProto();
    }

    //creates a new room
    [RemotingMethod]
    RealtimeRoomProto CreateRoom()
    {
        var room = roomController.CreateRoom();
        return room.GetProto();
    }

    //on close we must save player's profile
    protected override void OnClose(OnCloseEventArgs args)
    {
        dataStore.Update(this.credentials, this.profile).ContinueWith(t =>
        {
            logger.Error($"Couldn't save player profile #{credentials.Id}: {t.Exception}");
        }, TaskContinuationOptions.OnlyOnFaulted);
        base.OnClose(args);
    }
}