using System.Runtime.CompilerServices;

namespace UnrealSharp.CoreUObject;

public partial struct FTransform
{
    public FTransform(FQuat rotation, FVector location, FVector scale) : this()
    {
        Rotation = rotation;
        Translation = location;
        Scale3D = scale;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector TransformPosition(FVector v)
    {
        return Rotation.RotateVector(Scale3D * v) + Translation;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector TransformPositionNoScale(FVector v)
    {
        return Rotation.RotateVector(v) + Translation;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector TransformVector(FVector v)
    {
        return Rotation.RotateVector(Scale3D * v);
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
        return Rotation.Equals(other.Rotation) && Translation.Equals(other.Translation) && Scale3D.Equals(other.Scale3D);
    }

    public override bool Equals(object obj)
    {
        return obj is FTransform other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Rotation, Translation, Scale3D);
    }

    public override string ToString()
    {
        return $"Location: {Translation}, Rotation: {Rotation}, Scale: {Scale3D}";
    }

    public static bool operator ==(FTransform left, FTransform right) => left.Rotation == right.Rotation && left.Translation == right.Translation && left.Scale3D == right.Scale3D;
    public static bool operator !=(FTransform left, FTransform right) => !(left == right);
}
