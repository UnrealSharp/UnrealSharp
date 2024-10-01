using System.Globalization;
using System.Runtime.CompilerServices;

namespace UnrealSharp.CoreUObject;

public partial struct FPlane
{
    /// <summary>
    /// Constructs a FPlane from the X, Y, and Z components of its normal, and its distance from the origin on that normal.
    /// </summary>
    /// <param name="x">The X-component of the normal.</param>
    /// <param name="y">The Y-component of the normal.</param>
    /// <param name="z">The Z-component of the normal.</param>
    /// <param name="d">The distance of the FPlane along its normal from the origin.</param>
    public FPlane(double x, double y, double z, double d)
    {
        X = x;
        Y = y;
        Z = z;
        W = d;
    }

    /// <summary>
    /// Constructs a FPlane from the given normal and distance along the normal from the origin.
    /// </summary>
    /// <param name="normal">The FPlane's normal vector.</param>
    /// <param name="d">The FPlane's distance from the origin along its normal vector.</param>
    public FPlane(FVector normal, double d)
    {
        X = normal.X;
        Y = normal.Y;
        Z = normal.Z;
        W = d;
    }

    /// <summary>
    /// Constructs a FPlane from the given Vector4.
    /// </summary>
    /// <param name="value">A vector whose first 3 elements describe the normal vector, 
    /// and whose W component defines the distance along that normal from the origin.</param>
    public FPlane(FVector4 value)
    {
        X = value.X;
        Y = value.Y;
        Z = value.Z;
        W = value.W;
    }

    /// <summary>
    /// Creates a FPlane that contains the three given points.
    /// </summary>
    /// <param name="point1">The first point defining the FPlane.</param>
    /// <param name="point2">The second point defining the FPlane.</param>
    /// <param name="point3">The third point defining the FPlane.</param>
    /// <returns>The FPlane containing the three points.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FPlane CreateFromVertices(FVector point1, FVector point2, FVector point3)
    {
        double ax = point2.X - point1.X;
            double ay = point2.Y - point1.Y;
            double az = point2.Z - point1.Z;

            double bx = point3.X - point1.X;
            double by = point3.Y - point1.Y;
            double bz = point3.Z - point1.Z;

            // N=Cross(a,b)
            double nx = ay * bz - az * by;
            double ny = az * bx - ax * bz;
            double nz = ax * by - ay * bx;

            // Normalize(N)
            double ls = nx * nx + ny * ny + nz * nz;
            double invNorm = 1.0 / Math.Sqrt(ls);

            FVector normal = new FVector(
                nx * invNorm,
                ny * invNorm,
                nz * invNorm);

            return new FPlane(
                normal,
                -(normal.X * point1.X + normal.Y * point1.Y + normal.Z * point1.Z));
    }

    /// <summary>
    /// Creates a new FPlane whose normal vector is the source FPlane's normal vector normalized.
    /// </summary>
    /// <param name="value">The source FPlane.</param>
    /// <returns>The normalized FPlane.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FPlane Normalize(FPlane value)
    {
        const double FLT_EPSILON = 1.192092896e-07; // smallest such that 1.0+FLT_EPSILON != 1.0
        double f = value.X * value.X + value.Y * value.Y + value.Z * value.Z;

            if (Math.Abs(f - 1.0) < FLT_EPSILON)
            {
                return value; // It already normalized, so we don't need to further process.
            }

            double fInv = 1.0 / Math.Sqrt(f);

            return new FPlane(
                value.X * fInv,
                value.Y * fInv,
                value.Z * fInv,
                value.W * fInv);
    }

    /// <summary>
    ///  Transforms a normalized FPlane by a Quaternion rotation.
    /// </summary>
    /// <param name="FPlane"> The normalized FPlane to transform.
    /// This FPlane must already be normalized, so that its Normal vector is of unit length, before this method is called.</param>
    /// <param name="rotation">The Quaternion rotation to apply to the FPlane.</param>
    /// <returns>A new FPlane that results from applying the rotation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FPlane Transform(FPlane FPlane, FQuat rotation)
    {
        // Compute rotation matrix.
        double x2 = rotation.X + rotation.X;
        double y2 = rotation.Y + rotation.Y;
        double z2 = rotation.Z + rotation.Z;

        double wx2 = rotation.W * x2;
        double wy2 = rotation.W * y2;
        double wz2 = rotation.W * z2;
        double xx2 = rotation.X * x2;
        double xy2 = rotation.X * y2;
        double xz2 = rotation.X * z2;
        double yy2 = rotation.Y * y2;
        double yz2 = rotation.Y * z2;
        double zz2 = rotation.Z * z2;

        double m11 = 1.0 - yy2 - zz2;
        double m21 = xy2 - wz2;
        double m31 = xz2 + wy2;

        double m12 = xy2 + wz2;
        double m22 = 1.0 - xx2 - zz2;
        double m32 = yz2 - wx2;

        double m13 = xz2 - wy2;
        double m23 = yz2 + wx2;
        double m33 = 1.0 - xx2 - yy2;

        double x = FPlane.X, y = FPlane.Y, z = FPlane.Z;

        return new FPlane(
            x * m11 + y * m21 + z * m31,
            x * m12 + y * m22 + z * m32,
            x * m13 + y * m23 + z * m33,
            FPlane.W);
    }

    /// <summary>
    /// Calculates the dot product of a FPlane and Vector4.
    /// </summary>
    /// <param name="FPlane">The FPlane.</param>
    /// <param name="value">The Vector4.</param>
    /// <returns>The dot product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Dot(FPlane FPlane, FVector4 value)
    {
        return FPlane.X * value.X +
               FPlane.Y * value.Y +
               FPlane.Z * value.Z +
               FPlane.W * value.W;
    }

    /// <summary>
    /// Returns the dot product of a specified Vector and the normal vector of this FPlane plus the distance (D) value of the FPlane.
    /// </summary>
    /// <param name="FPlane">The FPlane.</param>
    /// <param name="value">The Vector.</param>
    /// <returns>The resulting value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double DotCoordinate(FPlane FPlane, FVector value)
    {
        return FPlane.X * value.X +
                   FPlane.Y * value.Y +
                   FPlane.Z * value.Z +
                   FPlane.W;
    }

    /// <summary>
    /// Returns the dot product of a specified Vector and the Normal vector of this FPlane.
    /// </summary>
    /// <param name="FPlane">The FPlane.</param>
    /// <param name="value">The Vector.</param>
    /// <returns>The resulting dot product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double DotNormal(FPlane FPlane, FVector value)
    {
        return FPlane.X * value.X +
                   FPlane.Y * value.Y +
                   FPlane.Z * value.Z;
    }

    /// <summary>
    /// Returns a boolean indicating whether the two given FPlanes are equal.
    /// </summary>
    /// <param name="value1">The first FPlane to compare.</param>
    /// <param name="value2">The second FPlane to compare.</param>
    /// <returns>True if the FPlanes are equal; False otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(FPlane value1, FPlane value2)
    {
        return (value1.X == value2.X &&
                value1.Y == value2.Y &&
                value1.Z == value2.Z &&
                value1.W == value2.W);
    }

    /// <summary>
    /// Returns a boolean indicating whether the two given FPlanes are not equal.
    /// </summary>
    /// <param name="value1">The first FPlane to compare.</param>
    /// <param name="value2">The second FPlane to compare.</param>
    /// <returns>True if the FPlanes are not equal; False if they are equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(FPlane value1, FPlane value2)
    {
        return (value1.X != value2.X ||
                value1.Y != value2.Y ||
                value1.Z != value2.Z ||
                value1.W != value2.W);
    }

    /// <summary>
    /// Returns a boolean indicating whether the given FPlane is equal to this FPlane instance.
    /// </summary>
    /// <param name="other">The FPlane to compare this instance to.</param>
    /// <returns>True if the other FPlane is equal to this instance; False otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(FPlane other)
    {
        return X == other.X &&
                    Y == other.Y &&
                    Z == other.Z &&
                    W == other.W;
    }

    /// <summary>
    /// Returns a boolean indicating whether the given Object is equal to this FPlane instance.
    /// </summary>
    /// <param name="obj">The Object to compare against.</param>
    /// <returns>True if the Object is equal to this FPlane; False otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object obj)
    {
        if (obj is FPlane)
        {
            return Equals((FPlane)obj);
        }

        return false;
    }

    /// <summary>
    /// Returns a String representing this FPlane instance.
    /// </summary>
    /// <returns>The string representation.</returns>
    public override string ToString()
    {
        CultureInfo ci = CultureInfo.CurrentCulture;
        return $"X={X.ToString(ci)}, Y={Y.ToString(ci)}, Z={Z.ToString(ci)}, W={W.ToString(ci)}";
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        return GetHashCode() + W.GetHashCode();
    }
}