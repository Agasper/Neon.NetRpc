using Neon.Networking.Udp.Messages;
using Neon.Rpc;

namespace Neon.ServerExample.Realtime.Rooms;

//Class represents real network player bound to the RpcSession
public class Player : IPlayer
{
    public int Id { get; }
    public Vector2 Position => position;

    Vector2 moveVector;
    Vector2 position;
    
    readonly PlayerSession session;

    public Player(int id, PlayerSession session)
    {
        this.Id = id;
        this.session = session;
        this.position = new Vector2(0.5f, 0.5f);
    }

    //Moves player to the specified position
    public void MoveTo(Vector2 position)
    {
        this.position = position;
    }

    //Adds move vector to be calculated on the next tick
    public void ApplyMoveVector(Vector2 vector2)
    {
        moveVector = vector2;
    }

    //Calls RPC method on the remote side
    public void Send<T>(string method, T arg, DeliveryType deliveryType)
    {
        _ = this.session.Send(method, arg, SendingOptions.Default.WithDeliveryType(DeliveryType.UnreliableSequenced));
    }

    //If move vector is not zero, move the player towards it
    public void Tick()
    {
        position += moveVector;
        if (position.x < 0)
            position.x = 0;
        if (position.y < 0)
            position.y = 0;
        if (position.x > 1)
            position.x = 1;
        if (position.y > 1)
            position.y = 1;
        moveVector = new Vector2(0, 0);
    }
}