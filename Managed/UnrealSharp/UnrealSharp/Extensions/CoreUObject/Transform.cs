using System.Runtime.CompilerServices;

namespace UnrealSharp.CoreUObject;

public partial struct FTransform
{
    public FTransform(FQuat rotation, FVector location, FVector scale) : this()
    {
        Rotation = rotation;
        Location = location;
        Scale = scale;
    }
    
    public FTransform(FRotator rotation, FVector location, FVector scale) : this()
    {
        Rotation = rotation.ToQuaternion();
        Location = location;
        Scale = scale;
    }
    
    public FQuat Rotation;
    public FVector Location;
    private double u0;
    public FVector Scale;
    private double u1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector TransformPosition(FVector v)
    {
        return Rotation.RotateVector(Scale * v) + Location;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector TransformPositionNoScale(FVector v)
    {
        return Rotation.RotateVector(v) + Location;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector TransformVector(FVector v)
    {
        return Rotation.RotateVector(Scale * v);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector TransformVectorNoScale(FVector v)
    {
        return Rotation.RotateVector(v);
    }

    public static readonly FTransform ZeroTransform = new(FQuat.Identity, FVector.Zero, FVector.Zero);
    public static readonly FTransform Identity = new(FQuat.Identity, FVector.Zero, FVector.One);
    
    public bool Equals(FTransform other)
    {
        return Rotation.Equals(other.Rotation) && Location.Equals(other.Location) && Scale.Equals(other.Scale);
    }

    public override bool Equals(object obj)
    {
        return obj is FTransform other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Rotation, Location, Scale);
    }

    public override string ToString()
    {
        return $"Location: {Location}, Rotation: {Rotation}, Scale: {Scale}";
    }

    public static bool operator ==(FTransform left, FTransform right) => left.Rotation == right.Rotation && left.Location == right.Location && left.Scale == right.Scale;
    public static bool operator !=(FTransform left, FTransform right) => !(left == right);
}
