using Neon.Networking.Udp.Messages;

namespace Neon.ServerExample.Realtime.Rooms;

public interface IPlayer
{
    int Id { get; }
    Vector2 Position { get; }
    void MoveTo(Vector2 position);

    void Send<T>(string method, T arg, DeliveryType deliveryType);
    void Tick();
}