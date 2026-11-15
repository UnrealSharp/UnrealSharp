namespace UnrealSharp.CoreUObject;

public partial record struct FVector4f
{
    public float X;
    public float Y;
    public float Z;
    public float W;

    public FVector4f(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }
}
