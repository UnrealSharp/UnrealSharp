using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace UnrealSharp.CoreUObject;

public partial struct Vector4
{
    /// <summary>
    /// Constructs a vector whose elements are all the single specified value.
    /// </summary>
    /// <param name="value">The element to fill the vector with.</param>
    public Vector4(double value) : this(value, value, value, value)
    {
    }
    
    /// <summary>
    /// Constructs a vector with the given individual elements.
    /// </summary>
    /// <param name="w">W component.</param>
    /// <param name="x">X component.</param>
    /// <param name="y">Y component.</param>
    /// <param name="z">Z component.</param>
    public Vector4(double x, double y, double z, double w)
    {
        W = w;
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Constructs a Vector4 from the given Vector2D and a Z and W component.
    /// </summary>
    /// <param name="value">The vector to use as the X and Y components.</param>
    /// <param name="z">The Z component.</param>
    /// <param name="w">The W component.</param>
    public Vector4(Vector2D value, double z, double w)
    {
        X = value.X;
        Y = value.Y;
        Z = z;
        W = w;
    }

    /// <summary>
    /// Constructs a Vector4 from the given Vector and a W component.
    /// </summary>
    /// <param name="value">The vector to use as the X, Y, and Z components.</param>
    /// <param name="w">The W component.</param>
    public Vector4(Vector value, double w)
    {
        X = value.X;
        Y = value.Y;
        Z = value.Z;
        W = w;
    }
    
    /// <summary>
    /// Copies the contents of the vector into the given array.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(double[] array)
    {
        CopyTo(array, 0);
    }

    /// <summary>
    /// Copies the contents of the vector into the given array, starting from index.
    /// </summary>
    /// <exception cref="ArgumentNullException">If array is null.</exception>
    /// <exception cref="RankException">If array is multidimensional.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If index is greater than end of the array or index is less than zero.</exception>
    /// <exception cref="ArgumentException">If number of elements in source vector is greater than those available in destination array.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(double[] array, int index)
    {
        if (array == null)
        {
            throw new NullReferenceException();
        }
        if (index < 0 || index >= array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index out of range");
        }
        if ((array.Length - index) < 4)
        {
            throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");
        }
        array[index] = X;
        array[index + 1] = Y;
        array[index + 2] = Z;
        array[index + 3] = W;
    }

    /// <summary>
    /// Returns a boolean indicating whether the given Vector4 is equal to this Vector4 instance.
    /// </summary>
    /// <param name="other">The Vector4 to compare this instance to.</param>
    /// <returns>True if the other Vector4 is equal to this instance; False otherwise.</returns>
    public bool Equals(Vector4 other)
    {
        return X == other.X && Y == other.Y && Z == other.Z && W == other.W;
    }

    /// <summary>
    /// Returns the dot product of two vectors.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="Vector2D">The second vector.</param>
    /// <returns>The dot product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Dot(Vector4 vector1, Vector4 Vector2D)
    {
        return vector1.X * Vector2D.X +
               vector1.Y * Vector2D.Y +
               vector1.Z * Vector2D.Z +
               vector1.W * Vector2D.W;
    }

    /// <summary>
    /// Returns a vector whose elements are the minimum of each of the pairs of elements in the two source vectors.
    /// </summary>
    /// <param name="value1">The first source vector.</param>
    /// <param name="value2">The second source vector.</param>
    /// <returns>The minimized vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Min(Vector4 value1, Vector4 value2)
    {
        return new Vector4(
            (value1.X < value2.X) ? value1.X : value2.X,
            (value1.Y < value2.Y) ? value1.Y : value2.Y,
            (value1.Z < value2.Z) ? value1.Z : value2.Z,
            (value1.W < value2.W) ? value1.W : value2.W);
    }

    /// <summary>
    /// Returns a vector whose elements are the maximum of each of the pairs of elements in the two source vectors.
    /// </summary>
    /// <param name="value1">The first source vector.</param>
    /// <param name="value2">The second source vector.</param>
    /// <returns>The maximized vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Max(Vector4 value1, Vector4 value2)
    {
        return new Vector4(
            (value1.X > value2.X) ? value1.X : value2.X,
            (value1.Y > value2.Y) ? value1.Y : value2.Y,
            (value1.Z > value2.Z) ? value1.Z : value2.Z,
            (value1.W > value2.W) ? value1.W : value2.W);
    }

    /// <summary>
    /// Returns a vector whose elements are the absolute values of each of the source vector's elements.
    /// </summary>
    /// <param name="value">The source vector.</param>
    /// <returns>The absolute value vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Abs(Vector4 value)
    {
        return new Vector4(Math.Abs(value.X), Math.Abs(value.Y), Math.Abs(value.Z), Math.Abs(value.W));
    }

    /// <summary>
    /// Returns a vector whose elements are the square root of each of the source vector's elements.
    /// </summary>
    /// <param name="value">The source vector.</param>
    /// <returns>The square root vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 SquareRoot(Vector4 value)
    {
        return new Vector4(Math.Sqrt(value.X), Math.Sqrt(value.Y), Math.Sqrt(value.Z), Math.Sqrt(value.W));
    }

    #region Public static operators
    /// <summary>
    /// Adds two vectors together.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The summed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator +(Vector4 left, Vector4 right)
    {
        return new Vector4(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
    }

    /// <summary>
    /// Subtracts the second vector from the first.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The difference vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator -(Vector4 left, Vector4 right)
    {
        return new Vector4(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
    }

    /// <summary>
    /// Multiplies two vectors together.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The product vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator *(Vector4 left, Vector4 right)
    {
        return new Vector4(left.X * right.X, left.Y * right.Y, left.Z * right.Z, left.W * right.W);
    }

    /// <summary>
    /// Multiplies a vector by the given scalar.
    /// </summary>
    /// <param name="left">The source vector.</param>
    /// <param name="right">The scalar value.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator *(Vector4 left, double right)
    {
        return left * new Vector4(right);
    }

    /// <summary>
    /// Multiplies a vector by the given scalar.
    /// </summary>
    /// <param name="left">The scalar value.</param>
    /// <param name="right">The source vector.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator *(double left, Vector4 right)
    {
        return new Vector4(left) * right;
    }

    /// <summary>
    /// Divides the first vector by the second.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The vector resulting from the division.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator /(Vector4 left, Vector4 right)
    {
        return new Vector4(left.X / right.X, left.Y / right.Y, left.Z / right.Z, left.W / right.W);
    }

    /// <summary>
    /// Divides the vector by the given scalar.
    /// </summary>
    /// <param name="value1">The source vector.</param>
    /// <param name="value2">The scalar value.</param>
    /// <returns>The result of the division.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator /(Vector4 value1, double value2)
    {
        double invDiv = 1.0 / value2;

        return new Vector4(
            value1.X * invDiv,
            value1.Y * invDiv,
            value1.Z * invDiv,
            value1.W * invDiv);
    }

    /// <summary>
    /// Negates a given vector.
    /// </summary>
    /// <param name="value">The source vector.</param>
    /// <returns>The negated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator -(Vector4 value)
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
    public static bool operator ==(Vector4 left, Vector4 right)
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
    public static bool operator !=(Vector4 left, Vector4 right)
    {
        return !(left == right);
    }
    #endregion Public static operators
    /// <summary>
    /// Returns the vector (0,0,0,0).
    /// </summary>
    public static Vector4 Zero { get { return new Vector4(); } }
    /// <summary>
    /// Returns the vector (1,1,1,1).
    /// </summary>
    public static Vector4 One { get { return new Vector4(1.0, 1.0, 1.0, 1.0); } }
    /// <summary>
    /// Returns the vector (1,0,0,0).
    /// </summary>
    public static Vector4 UnitX { get { return new Vector4(1.0, 0.0, 0.0, 0.0); } }
    /// <summary>
    /// Returns the vector (0,1,0,0).
    /// </summary>
    public static Vector4 UnitY { get { return new Vector4(0.0, 1.0, 0.0, 0.0); } }
    /// <summary>
    /// Returns the vector (0,0,1,0).
    /// </summary>
    public static Vector4 UnitZ { get { return new Vector4(0.0, 0.0, 1.0, 0.0); } }
    /// <summary>
    /// Returns the vector (0,0,0,1).
    /// </summary>
    public static Vector4 UnitW { get { return new Vector4(0.0, 0.0, 0.0, 1.0); } }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z, W);
    }

    /// <summary>
    /// Returns a boolean indicating whether the given Object is equal to this Vector4 instance.
    /// </summary>
    /// <param name="obj">The Object to compare against.</param>
    /// <returns>True if the Object is equal to this Vector4; False otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object obj)
    {
        if (!(obj is Vector4))
            return false;
        return Equals((Vector4)obj);
    }

    /// <summary>
    /// Returns a String representing this Vector4 instance.
    /// </summary>
    /// <returns>The string representation.</returns>
    public override string ToString()
    {
        return ToString("G", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Returns a String representing this Vector4 instance, using the specified format to format individual elements.
    /// </summary>
    /// <param name="format">The format of individual elements.</param>
    /// <returns>The string representation.</returns>
    public string ToString(string format)
    {
        return ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Returns a String representing this Vector4 instance, using the specified format to format individual elements 
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
        sb.Append(this.X.ToString(format, formatProvider));
        sb.Append(separator);
        sb.Append(' ');
        sb.Append(this.Y.ToString(format, formatProvider));
        sb.Append(separator);
        sb.Append(' ');
        sb.Append(this.Z.ToString(format, formatProvider));
        sb.Append(separator);
        sb.Append(' ');
        sb.Append(this.W.ToString(format, formatProvider));
        sb.Append('>');
        return sb.ToString();
    }

    /// <summary>
    /// Returns the length of the vector. This operation is cheaper than Length().
    /// </summary>
    /// <returns>The vector's length.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Length()
    {
        double ls = X * X + Y * Y + Z * Z + W * W;
        return Math.Sqrt(ls);
    }

    /// <summary>
    /// Returns the length of the vector squared.
    /// </summary>
    /// <returns>The vector's length squared.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double LengthSquared()
    {
        return X * X + Y * Y + Z * Z + W * W;
    }

    /// <summary>
    /// Returns the Euclidean distance between the two given points.
    /// </summary>
    /// <param name="value1">The first point.</param>
    /// <param name="value2">The second point.</param>
    /// <returns>The distance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Distance(Vector4 value1, Vector4 value2)
    {
        double dx = value1.X - value2.X;
        double dy = value1.Y - value2.Y;
        double dz = value1.Z - value2.Z;
        double dw = value1.W - value2.W;
        double ls = dx * dx + dy * dy + dz * dz + dw * dw;
        return Math.Sqrt(ls);
    }

    /// <summary>
    /// Returns the Euclidean distance squared between the two given points.
    /// </summary>
    /// <param name="value1">The first point.</param>
    /// <param name="value2">The second point.</param>
    /// <returns>The distance squared.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double DistanceSquared(Vector4 value1, Vector4 value2)
    {
        double dx = value1.X - value2.X;
        double dy = value1.Y - value2.Y;
        double dz = value1.Z - value2.Z;
        double dw = value1.W - value2.W;
        return dx * dx + dy * dy + dz * dz + dw * dw;
    }

    /// <summary>
    /// Returns a vector with the same direction as the given vector, but with a length of 1.
    /// </summary>
    /// <param name="vector">The vector to normalize.</param>
    /// <returns>The normalized vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Normalize(Vector4 vector)
    {
        double ls = vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z + vector.W * vector.W;
        double invNorm = 1.0 / Math.Sqrt(ls);
        return new Vector4(
                vector.X * invNorm,
                vector.Y * invNorm,
                vector.Z * invNorm,
                vector.W * invNorm);
    }

    /// <summary>
    /// Restricts a vector between a min and max value.
    /// </summary>
    /// <param name="value1">The source vector.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>The restricted vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Clamp(Vector4 value1, Vector4 min, Vector4 max)
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

        double w = value1.W;
        w = (w > max.W) ? max.W : w;
        w = (w < min.W) ? min.W : w;

        return new Vector4(x, y, z, w);
    }

    /// <summary>
    /// Linearly interpolates between two vectors based on the given weighting.
    /// </summary>
    /// <param name="value1">The first source vector.</param>
    /// <param name="value2">The second source vector.</param>
    /// <param name="amount">Value between 0 and 1 indicating the weight of the second source vector.</param>
    /// <returns>The interpolated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Lerp(Vector4 value1, Vector4 value2, double amount)
    {
        return new Vector4(
            value1.X + (value2.X - value1.X) * amount,
            value1.Y + (value2.Y - value1.Y) * amount,
            value1.Z + (value2.Z - value1.Z) * amount,
            value1.W + (value2.W - value1.W) * amount);
    }

    /// <summary>
    /// Transforms a vector by the given matrix.
    /// </summary>
    /// <param name="position">The source vector.</param>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Transform(Vector2D position, Matrix matrix)
    {
        return new Vector4(
            position.X * matrix.XPlane.X + position.Y * matrix.YPlane.X + matrix.WPlane.X,
            position.X * matrix.XPlane.Y + position.Y * matrix.YPlane.Y + matrix.WPlane.Y,
            position.X * matrix.XPlane.Z + position.Y * matrix.YPlane.Z + matrix.WPlane.Z,
            position.X * matrix.XPlane.W + position.Y * matrix.YPlane.W + matrix.WPlane.W);
    }

    /// <summary>
    /// Transforms a vector by the given matrix.
    /// </summary>
    /// <param name="position">The source vector.</param>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Transform(Vector position, Matrix matrix)
    {
        return new Vector4(
            position.X * matrix.XPlane.X + position.Y * matrix.YPlane.X + position.Z * matrix.ZPlane.X + matrix.WPlane.X,
            position.X * matrix.XPlane.Y + position.Y * matrix.YPlane.Y + position.Z * matrix.ZPlane.Y + matrix.WPlane.Y,
            position.X * matrix.XPlane.Z + position.Y * matrix.YPlane.Z + position.Z * matrix.ZPlane.Z + matrix.WPlane.Z,
            position.X * matrix.XPlane.W + position.Y * matrix.YPlane.W + position.Z * matrix.ZPlane.W + matrix.WPlane.W);
    }


    /// <summary>
    /// Transforms a vector by the given matrix.
    /// </summary>
    /// <param name="vector">The source vector.</param>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Transform(Vector4 vector, Matrix matrix)
    {
        return new Vector4(
            vector.X * matrix.XPlane.X + vector.Y * matrix.YPlane.X + vector.Z * matrix.ZPlane.X + vector.W * matrix.WPlane.X,
            vector.X * matrix.XPlane.Y + vector.Y * matrix.YPlane.Y + vector.Z * matrix.ZPlane.Y + vector.W * matrix.WPlane.Y,
            vector.X * matrix.XPlane.Z + vector.Y * matrix.YPlane.Z + vector.Z * matrix.ZPlane.Z + vector.W * matrix.WPlane.Z,
            vector.X * matrix.XPlane.W + vector.Y * matrix.YPlane.W + vector.Z * matrix.ZPlane.W + vector.W * matrix.WPlane.W);
    }


    /// <summary>
    /// Transforms a vector by the given Quat rotation value.
    /// </summary>
    /// <param name="value">The source vector to be rotated.</param>
    /// <param name="rotation">The rotation to apply.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Transform(Vector2D value, Quat rotation)
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

        return new Vector4(
            value.X * (1.0 - yy2 - zz2) + value.Y * (xy2 - wz2),
            value.X * (xy2 + wz2) + value.Y * (1.0 - xx2 - zz2),
            value.X * (xz2 - wy2) + value.Y * (yz2 + wx2),
            1.0);
    }

    /// <summary>
    /// Transforms a vector by the given Quat rotation value.
    /// </summary>
    /// <param name="value">The source vector to be rotated.</param>
    /// <param name="rotation">The rotation to apply.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Transform(Vector value, Quat rotation)
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

        return new Vector4(
            value.X * (1.0 - yy2 - zz2) + value.Y * (xy2 - wz2) + value.Z * (xz2 + wy2),
            value.X * (xy2 + wz2) + value.Y * (1.0 - xx2 - zz2) + value.Z * (yz2 - wx2),
            value.X * (xz2 - wy2) + value.Y * (yz2 + wx2) + value.Z * (1.0 - xx2 - yy2),
            1.0);
    }

    /// <summary>
    /// Transforms a vector by the given Quat rotation value.
    /// </summary>
    /// <param name="value">The source vector to be rotated.</param>
    /// <param name="rotation">The rotation to apply.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Transform(Vector4 value, Quat rotation)
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

        return new Vector4(
            value.X * (1.0 - yy2 - zz2) + value.Y * (xy2 - wz2) + value.Z * (xz2 + wy2),
            value.X * (xy2 + wz2) + value.Y * (1.0 - xx2 - zz2) + value.Z * (yz2 - wx2),
            value.X * (xz2 - wy2) + value.Y * (yz2 + wx2) + value.Z * (1.0 - xx2 - yy2),
            value.W);
    }

    // All these methods should be inlines as they are implemented
    // over JIT intrinsics

    /// <summary>
    /// Adds two vectors together.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The summed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Add(Vector4 left, Vector4 right)
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
    public static Vector4 Subtract(Vector4 left, Vector4 right)
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
    public static Vector4 Multiply(Vector4 left, Vector4 right)
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
    public static Vector4 Multiply(Vector4 left, double right)
    {
        return left * new Vector4(right, right, right, right);
    }

    /// <summary>
    /// Multiplies a vector by the given scalar.
    /// </summary>
    /// <param name="left">The scalar value.</param>
    /// <param name="right">The source vector.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Multiply(double left, Vector4 right)
    {
        return new Vector4(left, left, left, left) * right;
    }

    /// <summary>
    /// Divides the first vector by the second.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The vector resulting from the division.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Divide(Vector4 left, Vector4 right)
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
    public static Vector4 Divide(Vector4 left, double divisor)
    {
        return left / divisor;
    }

    /// <summary>
    /// Negates a given vector.
    /// </summary>
    /// <param name="value">The source vector.</param>
    /// <returns>The negated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Negate(Vector4 value)
    {
        return -value;
    }
}