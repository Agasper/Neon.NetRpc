using System.Collections.Concurrent;
using Google.Protobuf;
using Neon.Networking.Udp.Messages;
using Neon.Rpc;
using Neon.Rpc.Payload;
using Neon.ServerExample.Proto;

namespace Neon.ServerExample.Realtime.Rooms;

public class Room
{
    public Guid Guid { get; }
    public DateTime Created { get; }
    public bool Purgeable => playersCount == 0 && DateTime.UtcNow.Subtract(new TimeSpan(0, 1, 0)) > emptySince;

    List<IPlayer> players;
    int playersCount;
    int lastId;
    DateTime emptySince;
    bool closed;

    public Room()
    {
        this.players = new List<IPlayer>();
        this.Guid = Guid.NewGuid();
        this.Created = DateTime.UtcNow;
        this.emptySince = this.Created;
    }
    
    // Closing the room if there's no more players
    public bool TryClose()
    {
        lock (players)
        {
            if (playersCount > 0)
                return false;
            closed = true;
            return true;
        }
    }

    // Throw an exception if closed
    void CheckClosed()
    {
        if (closed)
            throw new InvalidOperationException("Room is closed");
    }

    //Fills the proto message
    public RealtimeRoomProto GetProto()
    {
        RealtimeRoomProto result = new RealtimeRoomProto();
        result.RoomGuid = ByteString.CopyFrom(this.Guid.ToByteArray());
        return result;
    }

    //Adds new AI player to the room
    public void AddBot()
    {
        lock (players)
        {
            CheckClosed();
            int newId = lastId++;
            AiPlayer aiPlayer = new AiPlayer(newId);
            players.Add(aiPlayer);
        }
    }
    
    //Removes first AI player found in the room or throws an exception
    public void RemoveBot()
    {
        lock (players)
        {
            CheckClosed();
            var bot = players.FirstOrDefault(p => p is AiPlayer);
            if (bot != null)
                players.Remove(bot);
            else
                throw new RemotingException("No more bots in the room", RemotingException.StatusCodeEnum.UserDefined);
        }
    }

    //Adding a real player to the room
    public Player Join(PlayerSession playerSession)
    {
        lock (players)
        {
            CheckClosed();
            int newId = lastId++;
            Player player = new Player(newId, playerSession);
            players.Add(player);
            emptySince = DateTime.MaxValue;
            playersCount++;
            return player;
        }
    }

    //Removing player from the room
    public void Leave(Player player)
    {
        lock (players)
        {
            if (!players.Remove(player))
                throw new InvalidOperationException($"Player not found");
            playersCount--;
            if (playersCount == 0)
                this.emptySince = DateTime.UtcNow;
        }
    }

    //Room tick, we must send the room state to all players
    //We must lock players every time we want access it for thread safety
    public void Tick()
    {
        RoomStateProto roomStateProto = new RoomStateProto();
        roomStateProto.Timestamp = DateTime.UtcNow.Ticks;
        lock (players)
        {
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                player.Tick();
                
                PlayerStateProto playerStateProto = new PlayerStateProto();
                playerStateProto.Id = player.Id;
                playerStateProto.X = player.Position.x;
                playerStateProto.Y = player.Position.y;
                
                roomStateProto.Players.Add(playerStateProto);
            }
            
            SendAll("RoomStateUpdate", roomStateProto, DeliveryType.UnreliableSequenced);
        }

    }

    public override string ToString()
    {
        return $"{nameof(Room)}[Guid={this.Guid}]";
    }

    //Calls RPC method on all the players
    void SendAll<T>(string method, T arg, DeliveryType deliveryType)
    {
        lock (players)
        {
            for (int i = 0; i < players.Count; i++)
            {
                players[i].Send(method, arg, deliveryType);
            }
        }
    }
}