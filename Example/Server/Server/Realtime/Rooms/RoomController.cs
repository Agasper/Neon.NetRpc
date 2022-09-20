using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Neon.Logging;
using Neon.ServerExample.Proto;
using Neon.Util.Polling;

namespace Neon.ServerExample.Realtime.Rooms;

/// <summary>
/// Controlling room creation & deletion
/// </summary>
public class RoomController : Pollable
{
    protected static ILogger logger = LogManager.Default.GetLogger("Datastore");
    
    //it must be thread safe
    ConcurrentDictionary<Guid, Room> rooms;
    DateTime nextPurge;

    public RoomController()
    {
        rooms = new ConcurrentDictionary<Guid, Room>();
        base.pollingThreadSleep = 50;
    }
    
    // Starts polling thread
    public void Start()
    {
        base.StartPolling();
    }
    
    // Stops polling thread
    public void Stop()
    {
        base.StopPolling(false);
    }

    public Room CreateRoom()
    {
        Room room = new Room();
        if (!rooms.TryAdd(room.Guid, room))
            throw new InvalidOperationException("Room guid collision");
        logger.Info($"Room {room} created!");
        return room;
    }

    //Filling proto message with all the rooms available
    public RealtimeRoomCollectionProto GetRoomsProto()
    {
        RealtimeRoomCollectionProto result = new RealtimeRoomCollectionProto();
        foreach (var pair in rooms)
        {
            result.Rooms.Add(pair.Value.GetProto());
        }

        return result;
    }

    public bool TryGetRoom(Guid guid, [MaybeNullWhen(false)] out Room room)
    {
        return rooms.TryGetValue(guid, out room);
    }

    //Polling thread tick
    protected override void PollEventsInternal()
    {
        try
        {
            DateTime now = DateTime.UtcNow;

            //Every 10 seconds we want to check rooms
            //And remove all rooms where .Purgeable is true
            //In other rooms call Tick()
            foreach (var pair in rooms)
            {
                if (now >= nextPurge && pair.Value.Purgeable)
                {
                    if (rooms.TryGetValue(pair.Key, out Room? room) && 
                        room.TryClose() &&
                        rooms.TryRemove(pair.Key, out _))
                    {
                        logger.Info($"Room {pair.Value} closed & deleted!");
                    }
                }
                else
                    pair.Value.Tick();
            }

            if (now >= nextPurge)
                nextPurge = now.AddSeconds(10);
        }
        catch (Exception ex)
        {
            logger.Error($"Unhandled exception in {nameof(RoomController)}.{nameof(PollEventsInternal)}: {ex}");
            throw;
        }
    }
}