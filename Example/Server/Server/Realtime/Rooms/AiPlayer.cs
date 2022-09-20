using Neon.Networking.Udp.Messages;

namespace Neon.ServerExample.Realtime.Rooms;

//Class represents AI player (bot)
public class AiPlayer : IPlayer
{
    public int Id { get; }
    public Vector2 Position { get; private set; }

    //current moving vector
    float movingX;
    float movingY;

    readonly Random rnd;

    public AiPlayer(int id)
    {
        this.rnd = new Random();
        this.Id = id;
        this.Position = new Vector2(0.5f, 0.5f);
        this.movingX = GetRandomInInterval(-1,1);
        this.movingY = GetRandomInInterval(-1,1);
    }
    
    public void MoveTo(Vector2 position)
    {
        this.Position = position;
    }

    public void Send<T>(string method, T arg, DeliveryType deliveryType)
    {
        
    }

    float GetRandomInInterval(float min, float max)
    {
        float delta = max - min;
        return min + delta * (float)rnd.NextDouble();
    }

    //On tick, move towards current moving vector, if hit bounds change the direction
    public void Tick()
    {
        var newPosition = Position + new Vector2(movingX, movingY) * 0.01f;
        if (newPosition.x < 0)
        {
            newPosition.x = 0;
            movingX = GetRandomInInterval(0.1f,1);
        }
        if (newPosition.x > 1)
        {
            newPosition.x = 1;
            movingX = GetRandomInInterval(-1f,-0.1f);
        }
        if (newPosition.y < 0)
        {
            newPosition.y = 0;
            movingY = GetRandomInInterval(0.1f,1);
        }
        if (newPosition.y > 1)
        {
            newPosition.y = 1;
            movingY = GetRandomInInterval(-1f,-0.1f);
        }
        
        this.MoveTo(newPosition);
    }
}