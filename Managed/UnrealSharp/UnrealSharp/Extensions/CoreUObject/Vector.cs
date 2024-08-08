using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace UnrealSharp.CoreUObject;

public partial struct FVector
{
    public FVector(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }
    
    public static FVector One => new(1, 1, 1);
    public static FVector Zero => new(0, 0, 0);
    
    public static FVector Forward => new(1, 0, 0);
    public static FVector Right => new(0, 1, 0);
    public static FVector Up => new(0, 0, 1);
    
    public static implicit operator System.Numerics.Vector3(FVector v) => new((float)v.X, (float)v.Y, (float)v.Z);
    public static implicit operator FVector(System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }

    /// <summary>
    /// Returns a boolean indicating whether the given Object is equal to this Vector instance.
    /// </summary>
    /// <param name="obj">The Object to compare against.</param>
    /// <returns>True if the Object is equal to this Vector; False otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object obj)
    {
        if (!(obj is FVector vector))
        {
            return false;  
        }

        return Equals(vector);
    }

    /// <summary>
    /// Returns a String representing this Vector instance.
    /// </summary>
    /// <returns>The string representation.</returns>
    public override string ToString()
    {
        return ToString("G", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Returns a String representing this Vector instance, using the specified format to format individual elements.
    /// </summary>
    /// <param name="format">The format of individual elements.</param>
    /// <returns>The string representation.</returns>
    public string ToString(string format)
    {
        return ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Returns a String representing this Vector instance, using the specified format to format individual elements 
    /// and the given IFormatProvider.
    /// </summary>
    /// <param name="format">The format of individual elements.</param>
    /// <param name="formatProvider">The format provider to use when formatting elements.</param>
    /// <returns>The string representation.</returns>
    public string ToString(string format, IFormatProvider formatProvider)
    {
        StringBuilder sb = new StringBuilder();
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        sb.Append('<');
        sb.Append(X.ToString(format, formatProvider));
        sb.Append(separator);
        sb.Append(' ');
        sb.Append(Y.ToString(format, formatProvider));
        sb.Append(separator);
        sb.Append(' ');
        sb.Append(Z.ToString(format, formatProvider));
        sb.Append('>');
        return sb.ToString();
    }

    /// <summary>
    /// Returns the length of the vector.
    /// </summary>
    /// <returns>The vector's length.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Length()
    {
        double ls = X * X + Y * Y + Z * Z;
        return Math.Sqrt(ls);
    }

    /// <summary>
    /// Returns the length of the vector squared. This operation is cheaper than Length().
    /// </summary>
    /// <returns>The vector's length squared.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double LengthSquared()
    {
        return X * X + Y * Y + Z * Z;
    }
    
    /// <summary>
    /// Returns the Euclidean distance between the two given points.
    /// </summary>
    /// <param name="value1">The first point.</param>
    /// <param name="value2">The second point.</param>
    /// <returns>The distance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Distance(FVector value1, FVector value2)
    {
        double dx = value1.X - value2.X;
        double dy = value1.Y - value2.Y;
        double dz = value1.Z - value2.Z;
        double ls = dx * dx + dy * dy + dz * dz;
        return Math.Sqrt(ls);
    }

    /// <summary>
    /// Returns the Euclidean distance squared between the two given points.
    /// </summary>
    /// <param name="value1">The first point.</param>
    /// <param name="value2">The second point.</param>
    /// <returns>The distance squared.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double DistanceSquared(FVector value1, FVector value2)
    {
        double dx = value1.X - value2.X;
        double dy = value1.Y - value2.Y;
        double dz = value1.Z - value2.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    /// <summary>
    /// Returns a vector with the same direction as the given vector, but with a length of 1.
    /// </summary>
    /// <param name="value">The vector to normalize.</param>
    /// <returns>The normalized vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector Normalize(FVector value)
    {
        double ls = value.X * value.X + value.Y * value.Y + value.Z * value.Z;
        double length = Math.Sqrt(ls);
        return new FVector(value.X / length, value.Y / length, value.Z / length);
    }

    /// <summary>
    /// Computes the cross product of two vectors.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>The cross product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector Cross(FVector vector1, FVector vector2)
    {
        return new FVector(
            vector1.Y * vector2.Z - vector1.Z * vector2.Y,
            vector1.Z * vector2.X - vector1.X * vector2.Z,
            vector1.X * vector2.Y - vector1.Y * vector2.X);
    }

    /// <summary>
    /// Returns the reflection of a vector off a surface that has the specified normal.
    /// </summary>
    /// <param name="vector">The source vector.</param>
    /// <param name="normal">The normal of the surface being reflected off.</param>
    /// <returns>The reflected vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector Reflect(FVector vector, FVector normal)
    {
        double dot = vector.X * normal.X + vector.Y * normal.Y + vector.Z * normal.Z;
            double tempX = normal.X * dot * 2;
            double tempY = normal.Y * dot * 2;
            double tempZ = normal.Z * dot * 2;
            return new FVector(vector.X - tempX, vector.Y - tempY, vector.Z - tempZ);
    }

    /// <summary>
    /// Restricts a vector between a min and max value.
    /// </summary>
    /// <param name="value1">The source vector.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>The restricted vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector Clamp(FVector value1, FVector min, FVector max)
    {
        // This compare order is very important!!!
        // We must follow HLSL behavior in the case user specified min value is bigger than max value.

        double x = value1.X;
        x = (x > max.X) ? max.X : x;
        x = (x < min.X) ? min.X : x;

        double y = value1.Y;
        y = (y > max.Y) ? max.Y : y;
        y = (y < min.Y) ? min.Y : y;

        double z = value1.Z;
        z = (z > max.Z) ? max.Z : z;
        z = (z < min.Z) ? min.Z : z;

        return new FVector(x, y, z);
    }

    /// <summary>
    /// Linearly interpolates between two vectors based on the given weighting.
    /// </summary>
    /// <param name="value1">The first source vector.</param>
    /// <param name="value2">The second source vector.</param>
    /// <param name="amount">Value between 0 and 1 indicating the weight of the second source vector.</param>
    /// <returns>The interpolated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector Lerp(FVector value1, FVector value2, double amount)
    {
        return new FVector(
                value1.X + (value2.X - value1.X) * amount,
                value1.Y + (value2.Y - value1.Y) * amount,
                value1.Z + (value2.Z - value1.Z) * amount);
    }

    /// <summary>
    /// Transforms a vector by the given matrix.
    /// </summary>
    /// <param name="position">The source vector.</param>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector Transform(FVector position, FMatrix matrix)
    {
        return new FVector(
            position.X * matrix.XPlane.X + position.Y * matrix.YPlane.X + position.Z * matrix.ZPlane.X + matrix.WPlane.X,
            position.X * matrix.XPlane.Y + position.Y * matrix.YPlane.Y + position.Z * matrix.ZPlane.Y + matrix.WPlane.Y,
            position.X * matrix.XPlane.Z + position.Y * matrix.YPlane.Z + position.Z * matrix.ZPlane.Z + matrix.WPlane.Z);
    }


    /// <summary>
    /// Transforms a vector normal by the given matrix.
    /// </summary>
    /// <param name="normal">The source vector.</param>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector TransformNormal(FVector normal, FMatrix matrix)
    {
        return new FVector(
            normal.X * matrix.XPlane.X + normal.Y * matrix.YPlane.X + normal.Z * matrix.ZPlane.X,
            normal.X * matrix.XPlane.Y + normal.Y * matrix.YPlane.Y + normal.Z * matrix.ZPlane.Y,
            normal.X * matrix.XPlane.Z + normal.Y * matrix.YPlane.Z + normal.Z * matrix.ZPlane.Z);
    }


    /// <summary>
    /// Transforms a vector by the given Quaternion rotation value.
    /// </summary>
    /// <param name="value">The source vector to be rotated.</param>
    /// <param name="rotation">The rotation to apply.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector Transform(FVector value, FQuat rotation)
    {
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

        return new FVector(
            value.X * (1.0 - yy2 - zz2) + value.Y * (xy2 - wz2) + value.Z * (xz2 + wy2),
            value.X * (xy2 + wz2) + value.Y * (1.0 - xx2 - zz2) + value.Z * (yz2 - wx2),
            value.X * (xz2 - wy2) + value.Y * (yz2 + wx2) + value.Z * (1.0 - xx2 - yy2));
    }

    // All these methods should be inlined as they are implemented
    // over JIT intrinsics

    /// <summary>
    /// Adds two vectors together.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The summed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector Add(FVector left, FVector right)
    {
        return left + right;
    }

    /// <summary>
    /// Subtracts the second vector from the first.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The difference vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector Subtract(FVector left, FVector right)
    {
        return left - right;
    }

    /// <summary>
    /// Multiplies two vectors together.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The product vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector Multiply(FVector left, FVector right)
    {
        return left * right;
    }

    /// <summary>
    /// Multiplies a vector by the given scalar.
    /// </summary>
    /// <param name="left">The source vector.</param>
    /// <param name="right">The scalar value.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector Multiply(FVector left, double right)
    {
        return left * right;
    }

    /// <summary>
    /// Multiplies a vector by the given scalar.
    /// </summary>
    /// <param name="left">The scalar value.</param>
    /// <param name="right">The source vector.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector Multiply(double left, FVector right)
    {
        return left * right;
    }

    /// <summary>
    /// Divides the first vector by the second.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The vector resulting from the division.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector Divide(FVector left, FVector right)
    {
        return left / right;
    }

    /// <summary>
    /// Divides the vector by the given scalar.
    /// </summary>
    /// <param name="left">The source vector.</param>
    /// <param name="divisor">The scalar value.</param>
    /// <returns>The result of the division.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector Divide(FVector left, double divisor)
    {
        return left / divisor;
    }

    /// <summary>
    /// Negates a given vector.
    /// </summary>
    /// <param name="value">The source vector.</param>
    /// <returns>The negated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector Negate(FVector value)
    {
        return -value;
    }
    
    public static bool operator == (FVector left, FVector right)
    {
        return left.X == right.X && left.Y == right.Y && left.Z == right.Z;
    }

    public static bool operator !=(FVector left, FVector right)
    {
        return !(left == right);
    }
    
    public static FVector operator +(FVector left, FVector right)
    {
        return new FVector(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }
    
    public static FVector operator -(FVector left, FVector right)
    {
        return new FVector(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }
    
    public static FVector operator -(FVector value)
    {
        return new FVector(-value.X, -value.Y, -value.Z);
    }
    
    public static FVector operator *(FVector left, FVector right)
    {
        return new FVector(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
    }
    
    public static FVector operator *(FVector left, double right)
    {
        return new FVector(left.X * right, left.Y * right, left.Z * right);
    }
    
    public static FVector operator *(double left, FVector right)
    {
        return right * left;
    }
    
    public static FVector operator /(FVector left, FVector right)
    {
        return new FVector(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
    }
    
    public static FVector operator /(FVector left, double right)
    {
        double invDiv = 1.0 / right;
        return new FVector(left.X * invDiv, left.Y * invDiv, left.Z * invDiv);
    }
    
    public static FVector operator /(double left, FVector right)
    {
        return new FVector(left / right.X, left / right.Y, left / right.Z);
    }
    
    public static FVector operator %(FVector left, FVector right)
    {
        return new FVector(left.X % right.X, left.Y % right.Y, left.Z % right.Z);
    }
    
    public static FVector operator %(FVector left, double right)
    {
        return new FVector(left.X % right, left.Y % right, left.Z % right);
    }
    
    public static FVector operator %(double left, FVector right)
    {
        return new FVector(left % right.X, left % right.Y, left % right.Z);
    }
}