using System.DoubleNumerics;
using System.Runtime.InteropServices;
using UnrealSharp.Attributes;

namespace UnrealSharp;

[UStruct(IsBlittable=true), StructLayout(LayoutKind.Sequential)]
public struct Transform(Quaternion rotation, Vector3 location, Vector3 scale)
{
    public Quaternion Rotation = rotation;
    public Vector3 Location = location;
    private double u0;
    public Vector3 Scale = scale;
    private double u1;

    public static readonly Transform ZeroTransform = new(Quaternion.Identity, Vector3.Zero, Vector3.One);
    
    public bool Equals(Transform other)
    {
        return Rotation.Equals(other.Rotation) && Location.Equals(other.Location) && Scale.Equals(other.Scale);
    }

    public override bool Equals(object obj)
    {
        return obj is Transform other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Rotation, Location, Scale);
    }

    public override string ToString()
    {
        return $"Location: {Location}, Rotation: {Rotation}, Scale: {Scale}";
    }
    
    public static bool operator == (Transform left, Transform right) => left.Rotation == right.Rotation && left.Location == right.Location && left.Scale == right.Scale; 
    public static bool operator != (Transform left, Transform right) => !(left == right);
}
