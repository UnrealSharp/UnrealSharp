using System.Numerics;

namespace UnrealSharp.CoreUObject;

public partial record struct FVector2f
{
    public float X;
    public float Y;

    public FVector2f(float x, float y)
    {
        X = x;
        Y = y;
    }

    public FVector2f(FVector2f vector)
    {
        X = vector.X;
        Y = vector.Y;
    }

    public FVector2f(Vector2 vector)
    {
        X = vector.X;
        Y = vector.Y;
    }

    public FVector2f(FVector vector)
    {
        X = (float)vector.X;
        Y = (float)vector.Y;
    }

    public static implicit operator FVector2f(FVector2D vector) => new FVector2f { X = (float)vector.X, Y = (float)vector.Y };
}
