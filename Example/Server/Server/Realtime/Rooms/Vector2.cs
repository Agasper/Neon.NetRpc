using System.Runtime.CompilerServices;

namespace Neon.ServerExample.Realtime.Rooms;

/// <summary>
/// Unity like simple vector struct
/// </summary>
public struct Vector2
{
    public float x;
    public float y;

    public Vector2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator+(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator-(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator*(Vector2 a, Vector2 b) { return new Vector2(a.x * b.x, a.y * b.y); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator/(Vector2 a, Vector2 b) { return new Vector2(a.x / b.x, a.y / b.y); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator-(Vector2 a) { return new Vector2(-a.x, -a.y); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator*(Vector2 a, float d) { return new Vector2(a.x * d, a.y * d); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator*(float d, Vector2 a) { return new Vector2(a.x * d, a.y * d); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator/(Vector2 a, float d) { return new Vector2(a.x / d, a.y / d); }

    public override string ToString()
    {
        return $"{nameof(Vector2)}[x={x},y={y}]";
    }
}