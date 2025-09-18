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

    public FTransform Inversed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            const double SmallNumber = 1e-8;

            // Invert the scale
            FVector invScale = GetSafeScaleReciprocal(Scale, SmallNumber);

            // Invert the rotation
            FQuat invRotation = FQuat.Inverse(Rotation);

            // Invert the translation
            FVector scaledTranslation = invScale * Location;
            FVector t2 = invRotation.RotateVector(scaledTranslation);
            FVector invTranslation = t2 * -1.0;

            return new FTransform(invRotation, invTranslation, invScale);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector TransformPosition(FVector v)
    {
        return Rotation.RotateVector(Scale * v) + Location;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector InverseTransformPosition(FVector v)
    {
        FQuat inverseRotation = FQuat.Inverse(Rotation);
        return inverseRotation.RotateVector(v - Location) / Scale;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector TransformPositionNoScale(FVector v)
    {
        return Rotation.RotateVector(v) + Location;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector InverseTransformPositionNoScale(FVector v)
    {
        FQuat inverseRotation = FQuat.Inverse(Rotation);
        return inverseRotation.RotateVector(v - Location);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector TransformVector(FVector v)
    {
        return Rotation.RotateVector(Scale * v);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector InverseTransformVector(FVector v)
    {
        FQuat inverseRotation = FQuat.Inverse(Rotation);
        return inverseRotation.RotateVector(v) / Scale;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector TransformVectorNoScale(FVector v)
    {
        return Rotation.RotateVector(v);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector InverseTransformVectorNoScale(FVector v)
    {
        FQuat inverseRotation = FQuat.Inverse(Rotation);
        return inverseRotation.RotateVector(v);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FVector GetSafeScaleReciprocal(FVector V, double tolerance)
    {
        return new FVector
        (
            Math.Abs(V.X) <= tolerance ? 0.0 : 1.0 / V.X,
            Math.Abs(V.Y) <= tolerance ? 0.0 : 1.0 / V.Y,
            Math.Abs(V.Z) <= tolerance ? 0.0 : 1.0 / V.Z
        );
    }

    public static readonly FTransform ZeroTransform = new(FQuat.Identity, FVector.Zero, FVector.Zero);
    public static readonly FTransform Identity = new(FQuat.Identity, FVector.Zero, FVector.One);
    
    public bool Equals(FTransform other)
    {
        return Rotation.Equals(other.Rotation) && Location.Equals(other.Location) && Scale.Equals(other.Scale);
    }

    public override bool Equals(object? obj)
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
