using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnrealSharp.CoreUObject;

[StructLayout(LayoutKind.Explicit, Size = 96)]
public partial record struct FTransform
{
    public FTransform(FQuat rotation, FVector location, FVector? scale = null) : this()
    {
        Rotation = rotation;
        Location = location;
        Scale = scale ?? FVector.One;
    }
    
    public FTransform(FRotator rotation, FVector location, FVector? scale = null) : this()
    {
        Rotation = rotation.ToQuaternion;
        Location = location;
        Scale = scale ?? FVector.One;
    }

    public FTransform()
    {
        Rotation = FQuat.Identity;
        Location = FVector.Zero;
        Scale = FVector.One;
    }
    
    [FieldOffset(0)]
    public FQuat Rotation;
    
    [FieldOffset(32)]
    public FVector Location;
    
    [FieldOffset(64)]
    public FVector Scale;

    public FTransform Inversed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            const double smallNumber = 1e-8;

            // Invert the scale
            FVector invScale = GetSafeScaleReciprocal(Scale, smallNumber);

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
    
    public static FTransform operator *(FTransform a, FTransform b)
    {
        FQuat combinedRotation = a.Rotation * b.Rotation;
        FVector combinedScale = a.Scale * b.Scale;
        FVector combinedLocation = a.TransformPosition(b.Location);
        return new FTransform(combinedRotation, combinedLocation, combinedScale);
    }
    
    public static FTransform operator *(FTransform a, FVector b)
    {
        return new FTransform(a.Rotation, a.TransformPosition(b), a.Scale);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Rotation, Location, Scale);
    }
}
