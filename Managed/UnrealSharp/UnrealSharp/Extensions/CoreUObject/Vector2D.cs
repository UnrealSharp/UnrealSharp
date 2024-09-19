using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace UnrealSharp.CoreUObject;

public partial struct FVector2D
{
    public FVector2D(double x, double y)
    {
        X = x;
        Y = y;
    }
    
    public FVector2D(FVector2D vec)
    {
        X = vec.X;
        Y = vec.Y;
    }

    public static implicit operator FVector2D(Vector2 vec) => new FVector2D(vec.X, vec.Y);
    public static implicit operator Vector2(FVector2D vec) => new Vector2((float)vec.X, (float)vec.Y);
    
    /// <summary>
    /// Returns the vector (0,0).
    /// </summary>
    public static FVector2D Zero => new();

    /// <summary>
    /// Returns the vector (1,1).
    /// </summary>
    public static FVector2D One => new(1.0, 1.0);

    /// <summary>
    /// Returns the vector (1,0).
    /// </summary>
    public static FVector2D UnitX => new(1.0, 0.0);

    /// <summary>
    /// Returns the vector (0,1).
    /// </summary>
    public static FVector2D UnitY => new(0.0, 1.0);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    /// <summary>
    /// Returns a boolean indicating whether the given Object is equal to this FVector2D instance.
    /// </summary>
    /// <param name="obj">The Object to compare against.</param>
    /// <returns>True if the Object is equal to this FVector2D; False otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object obj)
    {
        return obj is FVector2D FVector2D && Equals(FVector2D);
    }
    
    /// <summary>
    /// Returns a String representing this FVector2D instance.
    /// </summary>
    /// <returns>The string representation.</returns>
    public override string ToString()
    {
        return ToString("G", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Returns a String representing this FVector2D instance, using the specified format to format individual elements.
    /// </summary>
    /// <param name="format">The format of individual elements.</param>
    /// <returns>The string representation.</returns>
    public string ToString(string format)
    {
        return ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Returns a String representing this FVector2D instance, using the specified format to format individual elements 
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
        double ls = X * X + Y * Y;
        return Math.Sqrt(ls);
    }

    /// <summary>
    /// Returns the length of the vector squared. This operation is cheaper than Length().
    /// </summary>
    /// <returns>The vector's length squared.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double LengthSquared()
    {
        return X * X + Y * Y;
    }
    
    /// <summary>
    /// Returns the Euclidean distance between the two given points.
    /// </summary>
    /// <param name="value1">The first point.</param>
    /// <param name="value2">The second point.</param>
    /// <returns>The distance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Distance(FVector2D value1, FVector2D value2)
    {
        double dx = value1.X - value2.X;
            double dy = value1.Y - value2.Y;

            double ls = dx * dx + dy * dy;

            return Math.Sqrt(ls);
    }

    /// <summary>
    /// Returns the Euclidean distance squared between the two given points.
    /// </summary>
    /// <param name="value1">The first point.</param>
    /// <param name="value2">The second point.</param>
    /// <returns>The distance squared.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double DistanceSquared(FVector2D value1, FVector2D value2)
    {
        double dx = value1.X - value2.X;
        double dy = value1.Y - value2.Y;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Returns a vector with the same direction as the given vector, but with a length of 1.
    /// </summary>
    /// <param name="value">The vector to normalize.</param>
    /// <returns>The normalized vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D Normalize(FVector2D value)
    {
        double ls = value.X * value.X + value.Y * value.Y;
            double invNorm = 1.0 / (double)Math.Sqrt((double)ls);

            return new FVector2D(
                value.X * invNorm,
                value.Y * invNorm);
    }

    /// <summary>
    /// Returns the reflection of a vector off a surface that has the specified normal.
    /// </summary>
    /// <param name="vector">The source vector.</param>
    /// <param name="normal">The normal of the surface being reflected off.</param>
    /// <returns>The reflected vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D Reflect(FVector2D vector, FVector2D normal)
    {
        double dot = vector.X * normal.X + vector.Y * normal.Y;
        return new FVector2D(vector.X - 2.0 * dot * normal.X, vector.Y - 2.0 * dot * normal.Y);
    }

    /// <summary>
    /// Restricts a vector between a min and max value.
    /// </summary>
    /// <param name="value1">The source vector.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D Clamp(FVector2D value1, FVector2D min, FVector2D max)
    {
        // This compare order is very important!!!
        // We must follow HLSL behavior in the case user specified min value is bigger than max value.
        double x = value1.X;
        x = (x > max.X) ? max.X : x;
        x = (x < min.X) ? min.X : x;

        double y = value1.Y;
        y = (y > max.Y) ? max.Y : y;
        y = (y < min.Y) ? min.Y : y;

        return new FVector2D(x, y);
    }

    /// <summary>
    /// Linearly interpolates between two vectors based on the given weighting.
    /// </summary>
    /// <param name="value1">The first source vector.</param>
    /// <param name="value2">The second source vector.</param>
    /// <param name="amount">Value between 0 and 1 indicating the weight of the second source vector.</param>
    /// <returns>The interpolated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D Lerp(FVector2D value1, FVector2D value2, double amount)
    {
        return new FVector2D(
            value1.X + (value2.X - value1.X) * amount,
            value1.Y + (value2.Y - value1.Y) * amount);
    }

    /// <summary>
    /// Transforms a vector by the given matrix.
    /// </summary>
    /// <param name="position">The source vector.</param>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D Transform(FVector2D position, Matrix3x2 matrix)
    {
        return new FVector2D(
            position.X * matrix.M11 + position.Y * matrix.M21 + matrix.M31,
            position.X * matrix.M12 + position.Y * matrix.M22 + matrix.M32);
    }

    /// <summary>
    /// Transforms a vector by the given matrix.
    /// </summary>
    /// <param name="position">The source vector.</param>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D Transform(FVector2D position, FMatrix matrix)
    {
        return new FVector2D(
            position.X * matrix.XPlane.X + position.Y * matrix.YPlane.X + matrix.WPlane.X,
            position.X * matrix.XPlane.Y + position.Y * matrix.YPlane.Y + matrix.WPlane.Y);
    }


    /// <summary>
    /// Transforms a vector normal by the given matrix.
    /// </summary>
    /// <param name="normal">The source vector.</param>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D TransformNormal(FVector2D normal, Matrix3x2 matrix)
    {
        return new FVector2D(
            normal.X * matrix.M11 + normal.Y * matrix.M21,
            normal.X * matrix.M12 + normal.Y * matrix.M22);
    }

    /// <summary>
    /// Transforms a vector normal by the given matrix.
    /// </summary>
    /// <param name="normal">The source vector.</param>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D TransformNormal(FVector2D normal, Matrix4x4 matrix)
    {
        return new FVector2D(
            normal.X * matrix.M11 + normal.Y * matrix.M21,
            normal.X * matrix.M12 + normal.Y * matrix.M22);
    }

    /// <summary>
    /// Transforms a vector by the given Quaternion rotation value.
    /// </summary>
    /// <param name="value">The source vector to be rotated.</param>
    /// <param name="rotation">The rotation to apply.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D Transform(FVector2D value, Quaternion rotation)
    {
        double x2 = rotation.X + rotation.X;
        double y2 = rotation.Y + rotation.Y;
        double z2 = rotation.Z + rotation.Z;

        double wz2 = rotation.W * z2;
        double xx2 = rotation.X * x2;
        double xy2 = rotation.X * y2;
        double yy2 = rotation.Y * y2;
        double zz2 = rotation.Z * z2;

        return new FVector2D(
            value.X * (1.0 - yy2 - zz2) + value.Y * (xy2 - wz2),
            value.X * (xy2 + wz2) + value.Y * (1.0 - xx2 - zz2));
    }
    
    /// <summary>
    /// Copies the contents of the vector into the given array.
    /// </summary>
    /// <param name="array">The destination array.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(double[] array)
    {
        CopyTo(array, 0);
    }

    /// <summary>
    /// Copies the contents of the vector into the given array, starting from the given index.
    /// </summary>
    /// <exception cref="ArgumentNullException">If array is null.</exception>
    /// <exception cref="RankException">If array is multidimensional.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If index is greater than end of the array or index is less than zero.</exception>
    /// <exception cref="ArgumentException">If number of elements in source vector is greater than those available in destination array
    /// or if there are not enough elements to copy.</exception>
    public void CopyTo(double[] array, int index)
    {
        if (array == null)
        {
            throw new NullReferenceException();
        }
        if (index < 0 || index >= array.Length)
        {
            throw new ArgumentOutOfRangeException();
        }
        if ((array.Length - index) < 2)
        {
            throw new ArgumentException();
        }
        array[index] = X;
        array[index + 1] = Y;
    }

    /// <summary>
    /// Returns a boolean indicating whether the given FVector2D is equal to this FVector2D instance.
    /// </summary>
    /// <param name="other">The FVector2D to compare this instance to.</param>
    /// <returns>True if the other FVector2D is equal to this instance; False otherwise.</returns>
    public bool Equals(FVector2D other)
    {
        return this.X == other.X && this.Y == other.Y;
    }
    
    /// <summary>
    /// Returns the dot product of two vectors.
    /// </summary>
    /// <param name="value1">The first vector.</param>
    /// <param name="value2">The second vector.</param>
    /// <returns>The dot product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Dot(FVector2D value1, FVector2D value2)
    {
        return value1.X * value2.X +
               value1.Y * value2.Y;
    }

    /// <summary>
    /// Returns a vector whose elements are the minimum of each of the pairs of elements in the two source vectors.
    /// </summary>
    /// <param name="value1">The first source vector.</param>
    /// <param name="value2">The second source vector.</param>
    /// <returns>The minimized vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D Min(FVector2D value1, FVector2D value2)
    {
        return new FVector2D(
            (value1.X < value2.X) ? value1.X : value2.X,
            (value1.Y < value2.Y) ? value1.Y : value2.Y);
    }

    /// <summary>
    /// Returns a vector whose elements are the maximum of each of the pairs of elements in the two source vectors
    /// </summary>
    /// <param name="value1">The first source vector</param>
    /// <param name="value2">The second source vector</param>
    /// <returns>The maximized vector</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D Max(FVector2D value1, FVector2D value2)
    {
        return new FVector2D(
            (value1.X > value2.X) ? value1.X : value2.X,
            (value1.Y > value2.Y) ? value1.Y : value2.Y);
    }

    /// <summary>
    /// Returns a vector whose elements are the absolute values of each of the source vector's elements.
    /// </summary>
    /// <param name="value">The source vector.</param>
    /// <returns>The absolute value vector.</returns>        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D Abs(FVector2D value)
    {
        return new FVector2D(Math.Abs(value.X), Math.Abs(value.Y));
    }

    /// <summary>
    /// Returns a vector whose elements are the square root of each of the source vector's elements.
    /// </summary>
    /// <param name="value">The source vector.</param>
    /// <returns>The square root vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D SquareRoot(FVector2D value)
    {
        return new FVector2D(Math.Sqrt(value.X), Math.Sqrt(value.Y));
    }

    /// <summary>
    /// Adds two vectors together.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The summed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D operator +(FVector2D left, FVector2D right)
    {
        return new FVector2D(left.X + right.X, left.Y + right.Y);
    }

    /// <summary>
    /// Subtracts the second vector from the first.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The difference vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D operator -(FVector2D left, FVector2D right)
    {
        return new FVector2D(left.X - right.X, left.Y - right.Y);
    }

    /// <summary>
    /// Multiplies two vectors together.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The product vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D operator *(FVector2D left, FVector2D right)
    {
        return new FVector2D(left.X * right.X, left.Y * right.Y);
    }

    /// <summary>
    /// Multiplies a vector by the given scalar.
    /// </summary>
    /// <param name="left">The scalar value.</param>
    /// <param name="right">The source vector.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D operator *(double left, FVector2D right)
    {
        return new FVector2D(left, left) * right;
    }

    /// <summary>
    /// Multiplies a vector by the given scalar.
    /// </summary>
    /// <param name="left">The source vector.</param>
    /// <param name="right">The scalar value.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D operator *(FVector2D left, double right)
    {
        return left * new FVector2D(right, right);
    }

    /// <summary>
    /// Divides the first vector by the second.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The vector resulting from the division.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D operator /(FVector2D left, FVector2D right)
    {
        return new FVector2D(left.X / right.X, left.Y / right.Y);
    }

    /// <summary>
    /// Divides the vector by the given scalar.
    /// </summary>
    /// <param name="value1">The source vector.</param>
    /// <param name="value2">The scalar value.</param>
    /// <returns>The result of the division.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D operator /(FVector2D value1, double value2)
    {
        double invDiv = 1.0 / value2;
        return new FVector2D(
            value1.X * invDiv,
            value1.Y * invDiv);
    }

    /// <summary>
    /// Negates a given vector.
    /// </summary>
    /// <param name="value">The source vector.</param>
    /// <returns>The negated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector2D operator -(FVector2D value)
    {
        return Zero - value;
    }

    /// <summary>
    /// Returns a boolean indicating whether the two given vectors are equal.
    /// </summary>
    /// <param name="left">The first vector to compare.</param>
    /// <param name="right">The second vector to compare.</param>
    /// <returns>True if the vectors are equal; False otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(FVector2D left, FVector2D right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Returns a boolean indicating whether the two given vectors are not equal.
    /// </summary>
    /// <param name="left">The first vector to compare.</param>
    /// <param name="right">The second vector to compare.</param>
    /// <returns>True if the vectors are not equal; False if they are equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(FVector2D left, FVector2D right)
    {
        return !(left == right);
    }
}