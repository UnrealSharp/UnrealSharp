namespace UnrealSharp.CoreUObject;

public partial record struct FVector3f
{
    public float X;
    public float Y;
    public float Z;

    public FVector3f(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
