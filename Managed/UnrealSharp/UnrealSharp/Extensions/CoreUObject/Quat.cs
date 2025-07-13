using System.Globalization;
using UnrealSharp.Interop;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.CoreUObject;

public partial struct FQuat
{
    /// <summary>
    /// Returns a Quat representing no rotation. 
    /// </summary>
    public static FQuat Identity => new(0, 0, 0, 1);

    /// <summary>
    /// Returns whether the Quat is the identity Quat.
    /// </summary>
    public bool IsIdentity => X == 0 && Y == 0 && Z == 0 && W == 1;
    
    /// <summary>
    /// Constructs a Quat from the given Rotator.
    /// </summary>
    public FQuat(FRotator rotator)
    {
        UCSQuatExtensions.ToQuaternion(out this, this);
    }
    
    /// <summary>
    /// Constructs a Quat from the given components.
    /// </summary>
    /// <param name="x">The X component of the Quat.</param>
    /// <param name="y">The Y component of the Quat.</param>
    /// <param name="z">The Z component of the Quat.</param>
    /// <param name="w">The W component of the Quat.</param>
    public FQuat(double x, double y, double z, double w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }
    
    /// <summary>
    /// Constructs a Quat from the given vector and rotation parts.
    /// </summary>
    /// <param name="vectorPart">The vector part of the Quat.</param>
    /// <param name="scalarPart">The rotation part of the Quat.</param>
    public FQuat(FVector vectorPart, double scalarPart)
    {
        X = vectorPart.X;
        Y = vectorPart.Y;
        Z = vectorPart.Z;
        W = scalarPart;
    }
    
    /// <summary>
    /// Returns a Rotator view of this Quat.
    /// </summary>
    public FRotator ToRotator()
    {
        UCSQuatExtensions.ToRotator(out var rotator, this);
        return rotator;
    }

    /// <summary>
    /// Calculates the length of the Quat.
    /// </summary>
    /// <returns>The computed length of the Quat.</returns>
    public double Length()
    {
        double ls = X * X + Y * Y + Z * Z + W * W;

        return Math.Sqrt(ls);
    }

    /// <summary>
    /// Calculates the length squared of the Quat. This operation is cheaper than Length().
    /// </summary>
    /// <returns>The length squared of the Quat.</returns>
    public double LengthSquared()
    {
        return X * X + Y * Y + Z * Z + W * W;
    }

    /// <summary>
    /// Rotates a vector by the Quat.
    /// </summary>
    /// <param name="v">The vector to rotate</param>
    /// <returns>The rotated vector resulting from applying the Quaterinion</returns>
    public FVector RotateVector(FVector v)
    {
        // http://people.csail.mit.edu/bkph/articles/Quaternions.pdf
        // V' = V + 2w(Q x V) + (2Q x (Q x V))
        // refactor:
        // V' = V + w(2(Q x V)) + (Q x (2(Q x V)))
        // T = 2(Q x V);
        // V' = V + w*(T) + (Q x T)

        FVector q = new FVector(X, Y, Z);
        FVector tt = 2f * FVector.Cross(q, v);
        FVector result = v + W * tt + FVector.Cross(q, tt);
        return result;
    }

    /// <summary>
    /// Divides each component of the Quat by the length of the Quat.
    /// </summary>
    /// <param name="value">The source Quat.</param>
    /// <returns>The normalized Quat.</returns>
    public static FQuat Normalize(FQuat value)
    {
        double ls = value.X * value.X + value.Y * value.Y + value.Z * value.Z + value.W * value.W;

        double invNorm = 1.0 / Math.Sqrt(ls);
        
        return new FQuat
        {
            X = value.X * invNorm,
            Y = value.Y * invNorm,
            Z = value.Z * invNorm,
            W = value.W * invNorm
        };;
    }

    /// <summary>
    /// Creates the conjugate of a specified Quat.
    /// </summary>
    /// <param name="value">The Quat of which to return the conjugate.</param>
    /// <returns>A new Quat that is the conjugate of the specified one.</returns>
    public static FQuat Conjugate(FQuat value)
    {
         return  new FQuat
        {
            X = -value.X,
            Y = -value.Y,
            Z = -value.Z,
            W = value.W
        };
    }

    /// <summary>
    /// Returns the inverse of a Quat.
    /// </summary>
    /// <param name="value">The source Quat.</param>
    /// <returns>The inverted Quat.</returns>
    public static FQuat Inverse(FQuat value)
    {
        //  -1   (       a              -v       )
        // q   = ( -------------   ------------- )
        //       (  a^2 + |v|^2  ,  a^2 + |v|^2  )

        double ls = value.X * value.X + value.Y * value.Y + value.Z * value.Z + value.W * value.W;
        double invNorm = 1.0 / ls;
        
        return new FQuat
        {
            X = -value.X * invNorm,
            Y = -value.Y * invNorm,
            Z = -value.Z * invNorm,
            W = value.W * invNorm
        };
    }

    /// <summary>
    /// Creates a Quat from a normalized vector axis and an angle to rotate about the vector.
    /// </summary>
    /// <param name="axis">The unit vector to rotate around.
    /// This vector must be normalized before calling this function or the resulting Quat will be incorrect.</param>
    /// <param name="angle">The angle, in radians, to rotate around the vector.</param>
    /// <returns>The created Quat.</returns>
    public static FQuat CreateFromAxisAngle(FVector axis, double angle)
    {
        double halfAngle = angle * 0.5;
        double s = Math.Sin(halfAngle);
        double c = Math.Cos(halfAngle);

        
        return new FQuat
        {
            X = axis.X * s,
            Y = axis.Y * s,
            Z = axis.Z * s,
            W = c
        };
    }

    /// <summary>
    /// Creates a new Quat from the given yaw, pitch, and roll, in radians.
    /// </summary>
    /// <param name="yaw">The yaw angle, in radians, around the Y-axis.</param>
    /// <param name="pitch">The pitch angle, in radians, around the X-axis.</param>
    /// <param name="roll">The roll angle, in radians, around the Z-axis.</param>
    /// <returns></returns>
    public static FQuat CreateFromYawPitchRoll(double yaw, double pitch, double roll)
    {
        //  Roll first, about axis the object is facing, then
        //  pitch upward, then yaw to face into the new heading
        double halfRoll = roll * 0.5;
        var sr = Math.Sin(halfRoll);
        var cr = Math.Cos(halfRoll);

        double halfPitch = pitch * 0.5;
        var sp = Math.Sin(halfPitch);
        var cp = Math.Cos(halfPitch);

        double halfYaw = yaw * 0.5;
        var sy = Math.Sin(halfYaw);
        var cy = Math.Cos(halfYaw);

       return new FQuat
       {
            X = cy * sp * cr + sy * cp * sr,
            Y = sy * cp * cr - cy * sp * sr,
            Z = cy * cp * sr - sy * sp * cr,
            W = cy * cp * cr + sy * sp * sr
        };
    }

    /// <summary>
    /// Calculates the dot product of two Quats.
    /// </summary>
    /// <param name="Quat1">The first source Quat.</param>
    /// <param name="Quat2">The second source Quat.</param>
    /// <returns>The dot product of the Quats.</returns>
    public static double Dot(FQuat Quat1, FQuat Quat2)
    {
        return Quat1.X * Quat2.X +
               Quat1.Y * Quat2.Y +
               Quat1.Z * Quat2.Z +
               Quat1.W * Quat2.W;
    }

    /// <summary>
    /// Interpolates between two Quats, using spherical linear interpolation.
    /// </summary>
    /// <param name="Quat1">The first source Quat.</param>
    /// <param name="Quat2">The second source Quat.</param>
    /// <param name="amount">The relative weight of the second source Quat in the interpolation.</param>
    /// <returns>The interpolated Quat.</returns>
    public static FQuat Slerp(FQuat Quat1, FQuat Quat2, double amount)
    {
        const double epsilon = 1e-6;

        double t = amount;

        double cosOmega = Quat1.X * Quat2.X + Quat1.Y * Quat2.Y +
                         Quat1.Z * Quat2.Z + Quat1.W * Quat2.W;

        bool flip = false;

        if (cosOmega < 0.0)
        {
            flip = true;
            cosOmega = -cosOmega;
        }

        double s1, s2;

        if (cosOmega > (1.0 - epsilon))
        {
            // Too close, do straight linear interpolation.
            s1 = 1.0 - t;
            s2 = (flip) ? -t : t;
        }
        else
        {
            double omega = Math.Acos(cosOmega);
            double invSinOmega = 1 / Math.Sin(omega);

            s1 = Math.Sin((1.0 - t) * omega) * invSinOmega;
            s2 = (flip)
                ? -Math.Sin(t * omega) * invSinOmega
                : Math.Sin(t * omega) * invSinOmega;
        }

        return new FQuat
        {
            X = s1 * Quat1.X + s2 * Quat2.X,
            Y = s1 * Quat1.Y + s2 * Quat2.Y,
            Z = s1 * Quat1.Z + s2 * Quat2.Z,
            W = s1 * Quat1.W + s2 * Quat2.W
        };
    }

    /// <summary>
    ///  Linearly interpolates between two Quats.
    /// </summary>
    /// <param name="Quat1">The first source Quat.</param>
    /// <param name="Quat2">The second source Quat.</param>
    /// <param name="amount">The relative weight of the second source Quat in the interpolation.</param>
    /// <returns>The interpolated Quat.</returns>
    public static FQuat Lerp(FQuat Quat1, FQuat Quat2, double amount)
    {
        double t = amount;
        double t1 = 1.0 - t;

        FQuat r = new FQuat();

        double dot = Quat1.X * Quat2.X + Quat1.Y * Quat2.Y +
                    Quat1.Z * Quat2.Z + Quat1.W * Quat2.W;

        if (dot >= 0.0)
        {
            r.X = t1 * Quat1.X + t * Quat2.X;
            r.Y = t1 * Quat1.Y + t * Quat2.Y;
            r.Z = t1 * Quat1.Z + t * Quat2.Z;
            r.W = t1 * Quat1.W + t * Quat2.W;
        }
        else
        {
            r.X = t1 * Quat1.X - t * Quat2.X;
            r.Y = t1 * Quat1.Y - t * Quat2.Y;
            r.Z = t1 * Quat1.Z - t * Quat2.Z;
            r.W = t1 * Quat1.W - t * Quat2.W;
        }

        // Normalize it.
        double ls = r.X * r.X + r.Y * r.Y + r.Z * r.Z + r.W * r.W;
        double invNorm = 1.0 / Math.Sqrt(ls);

        r.X *= invNorm;
        r.Y *= invNorm;
        r.Z *= invNorm;
        r.W *= invNorm;

        return r;
    }

    /// <summary>
    /// Concatenates two Quats; the result represents the value1 rotation followed by the value2 rotation.
    /// </summary>
    /// <param name="value1">The first Quat rotation in the series.</param>
    /// <param name="value2">The second Quat rotation in the series.</param>
    /// <returns>A new Quat representing the concatenation of the value1 rotation followed by the value2 rotation.</returns>
    public static FQuat Concatenate(FQuat value1, FQuat value2)
    {
        // Concatenate rotation is actually q2 * q1 instead of q1 * q2.
        // So that's why value2 goes q1 and value1 goes q2.
        double q1x = value2.X;
        double q1y = value2.Y;
        double q1z = value2.Z;
        double q1w = value2.W;

        double q2x = value1.X;
        double q2y = value1.Y;
        double q2z = value1.Z;
        double q2w = value1.W;

        // cross(av, bv)
        double cx = q1y * q2z - q1z * q2y;
        double cy = q1z * q2x - q1x * q2z;
        double cz = q1x * q2y - q1y * q2x;

        double dot = q1x * q2x + q1y * q2y + q1z * q2z;
        
        return new FQuat
        {
            X = q1x * q2w + q2x * q1w + cx,
            Y = q1y * q2w + q2y * q1w + cy,
            Z = q1z * q2w + q2z * q1w + cz,
            W = q1w * q2w - dot
        };
    }

    /// <summary>
    /// Flips the sign of each component of the Quat.
    /// </summary>
    /// <param name="value">The source Quat.</param>
    /// <returns>The negated Quat.</returns>
    public static FQuat Negate(FQuat value)
    {
        return new FQuat
        {
            X = -value.X,
            Y = -value.Y,
            Z = -value.Z,
            W = -value.W
        };
    }

    /// <summary>
    /// Adds two Quats element-by-element.
    /// </summary>
    /// <param name="value1">The first source Quat.</param>
    /// <param name="value2">The second source Quat.</param>
    /// <returns>The result of adding the Quats.</returns>
    public static FQuat Add(FQuat value1, FQuat value2)
    {
        return new FQuat
        {
            X = value1.X + value2.X,
            Y = value1.Y + value2.Y,
            Z = value1.Z + value2.Z,
            W = value1.W + value2.W
        };
    }

    /// <summary>
    /// Subtracts one Quat from another.
    /// </summary>
    /// <param name="value1">The first source Quat.</param>
    /// <param name="value2">The second Quat, to be subtracted from the first.</param>
    /// <returns>The result of the subtraction.</returns>
    public static FQuat Subtract(FQuat value1, FQuat value2)
    {
        return new FQuat
        {
            X = value1.X - value2.X,
            Y = value1.Y - value2.Y,
            Z = value1.Z - value2.Z,
            W = value1.W - value2.W
        };
    }

    /// <summary>
    /// Multiplies two Quats together.
    /// </summary>
    /// <param name="value1">The Quat on the left side of the multiplication.</param>
    /// <param name="value2">The Quat on the right side of the multiplication.</param>
    /// <returns>The result of the multiplication.</returns>
    public static FQuat Multiply(FQuat value1, FQuat value2)
    {
        double q1x = value1.X;
        double q1y = value1.Y;
        double q1z = value1.Z;
        double q1w = value1.W;

        double q2x = value2.X;
        double q2y = value2.Y;
        double q2z = value2.Z;
        double q2w = value2.W;

        // cross(av, bv)
        double cx = q1y * q2z - q1z * q2y;
        double cy = q1z * q2x - q1x * q2z;
        double cz = q1x * q2y - q1y * q2x;

        double dot = q1x * q2x + q1y * q2y + q1z * q2z;

        return new FQuat
        {
            X = q1x * q2w + q2x * q1w + cx,
            Y = q1y * q2w + q2y * q1w + cy,
            Z = q1z * q2w + q2z * q1w + cz,
            W = q1w * q2w - dot
        };
    }

    /// <summary>
    /// Multiplies a Quat by a scalar value.
    /// </summary>
    /// <param name="value1">The source Quat.</param>
    /// <param name="value2">The scalar value.</param>
    /// <returns>The result of the multiplication.</returns>
    public static FQuat Multiply(FQuat value1, double value2)
    {
        return new FQuat
        {
            X = value1.X * value2,
            Y = value1.Y * value2,
            Z = value1.Z * value2,
            W = value1.W * value2
        };
    }

    /// <summary>
    /// Divides a Quat by another Quat.
    /// </summary>
    /// <param name="value1">The source Quat.</param>
    /// <param name="value2">The divisor.</param>
    /// <returns>The result of the division.</returns>
    public static FQuat Divide(FQuat value1, FQuat value2)
    {
        double q1x = value1.X;
        double q1y = value1.Y;
        double q1z = value1.Z;
        double q1w = value1.W;

        //-------------------------------------
        // Inverse part.
        double ls = value2.X * value2.X + value2.Y * value2.Y +
                   value2.Z * value2.Z + value2.W * value2.W;
        double invNorm = 1.0 / ls;

        double q2x = -value2.X * invNorm;
        double q2y = -value2.Y * invNorm;
        double q2z = -value2.Z * invNorm;
        double q2w = value2.W * invNorm;

        //-------------------------------------
        // Multiply part.

        // cross(av, bv)
        double cx = q1y * q2z - q1z * q2y;
        double cy = q1z * q2x - q1x * q2z;
        double cz = q1x * q2y - q1y * q2x;

        double dot = q1x * q2x + q1y * q2y + q1z * q2z;
        
        return new FQuat
        {
            X = q1x * q2w + q2x * q1w + cx,
            Y = q1y * q2w + q2y * q1w + cy,
            Z = q1z * q2w + q2z * q1w + cz,
            W = q1w * q2w - dot
        };
    }

    /// <summary>
    /// Flips the sign of each component of the Quat.
    /// </summary>
    /// <param name="value">The source Quat.</param>
    /// <returns>The negated Quat.</returns>
    public static FQuat operator -(FQuat value)
    {
        return new FQuat
        {
            X = -value.X,
            Y = -value.Y,
            Z = -value.Z,
            W = -value.W
        };
    }

    /// <summary>
    /// Adds two Quats element-by-element.
    /// </summary>
    /// <param name="value1">The first source Quat.</param>
    /// <param name="value2">The second source Quat.</param>
    /// <returns>The result of adding the Quats.</returns>
    public static FQuat operator +(FQuat value1, FQuat value2)
    {
        return new FQuat
        {
            X = value1.X + value2.X,
            Y = value1.Y + value2.Y,
            Z = value1.Z + value2.Z,
            W = value1.W + value2.W
        };
    }

    /// <summary>
    /// Subtracts one Quat from another.
    /// </summary>
    /// <param name="value1">The first source Quat.</param>
    /// <param name="value2">The second Quat, to be subtracted from the first.</param>
    /// <returns>The result of the subtraction.</returns>
    public static FQuat operator -(FQuat value1, FQuat value2)
    {
        return new FQuat
        {
            X = value1.X - value2.X,
            Y = value1.Y - value2.Y,
            Z = value1.Z - value2.Z,
            W = value1.W - value2.W
        };
    }

    /// <summary>
    /// Multiplies two Quats together.
    /// </summary>
    /// <param name="value1">The Quat on the left side of the multiplication.</param>
    /// <param name="value2">The Quat on the right side of the multiplication.</param>
    /// <returns>The result of the multiplication.</returns>
    public static FQuat operator *(FQuat value1, FQuat value2)
    {
        double q1x = value1.X;
        double q1y = value1.Y;
        double q1z = value1.Z;
        double q1w = value1.W;

        double q2x = value2.X;
        double q2y = value2.Y;
        double q2z = value2.Z;
        double q2w = value2.W;

        // cross(av, bv)
        double cx = q1y * q2z - q1z * q2y;
        double cy = q1z * q2x - q1x * q2z;
        double cz = q1x * q2y - q1y * q2x;

        double dot = q1x * q2x + q1y * q2y + q1z * q2z;
        
        return new FQuat
        {
            X = q1x * q2w + q2x * q1w + cx,
            Y = q1y * q2w + q2y * q1w + cy,
            Z = q1z * q2w + q2z * q1w + cz,
            W = q1w * q2w - dot
        };
    }

    /// <summary>
    /// Multiplies a Quat by a scalar value.
    /// </summary>
    /// <param name="value1">The source Quat.</param>
    /// <param name="value2">The scalar value.</param>
    /// <returns>The result of the multiplication.</returns>
    public static FQuat operator *(FQuat value1, double value2)
    {
        return new FQuat
        {
            X = value1.X * value2,
            Y = value1.Y * value2,
            Z = value1.Z * value2,
            W = value1.W * value2
        };
    }

    /// <summary>
    /// Divides a Quat by another Quat.
    /// </summary>
    /// <param name="value1">The source Quat.</param>
    /// <param name="value2">The divisor.</param>
    /// <returns>The result of the division.</returns>
    public static FQuat operator /(FQuat value1, FQuat value2)
    {
        double q1x = value1.X;
        double q1y = value1.Y;
        double q1z = value1.Z;
        double q1w = value1.W;

        //-------------------------------------
        // Inverse part.
        double ls = value2.X * value2.X + value2.Y * value2.Y +
                   value2.Z * value2.Z + value2.W * value2.W;
        double invNorm = 1.0 / ls;

        double q2x = -value2.X * invNorm;
        double q2y = -value2.Y * invNorm;
        double q2z = -value2.Z * invNorm;
        double q2w = value2.W * invNorm;

        //-------------------------------------
        // Multiply part.

        // cross(av, bv)
        double cx = q1y * q2z - q1z * q2y;
        double cy = q1z * q2x - q1x * q2z;
        double cz = q1x * q2y - q1y * q2x;

        double dot = q1x * q2x + q1y * q2y + q1z * q2z;
        
        return new FQuat
        {
            X = q1x * q2w + q2x * q1w + cx,
            Y = q1y * q2w + q2y * q1w + cy,
            Z = q1z * q2w + q2z * q1w + cz,
            W = q1w * q2w - dot
        };
    }

    /// <summary>
    /// Returns a boolean indicating whether the two given Quats are equal.
    /// </summary>
    /// <param name="value1">The first Quat to compare.</param>
    /// <param name="value2">The second Quat to compare.</param>
    /// <returns>True if the Quats are equal; False otherwise.</returns>
    public static bool operator ==(FQuat value1, FQuat value2)
    {
        return (value1.X == value2.X &&
                value1.Y == value2.Y &&
                value1.Z == value2.Z &&
                value1.W == value2.W);
    }

    /// <summary>
    /// Returns a boolean indicating whether the two given Quats are not equal.
    /// </summary>
    /// <param name="value1">The first Quat to compare.</param>
    /// <param name="value2">The second Quat to compare.</param>
    /// <returns>True if the Quats are not equal; False if they are equal.</returns>
    public static bool operator !=(FQuat value1, FQuat value2)
    {
        return (value1.X != value2.X ||
                value1.Y != value2.Y ||
                value1.Z != value2.Z ||
                value1.W != value2.W);
    }

    /// <summary>
    /// Returns a boolean indicating whether the given Quat is equal to this Quat instance.
    /// </summary>
    /// <param name="other">The Quat to compare this instance to.</param>
    /// <returns>True if the other Quat is equal to this instance; False otherwise.</returns>
    public bool Equals(FQuat other)
    {
        return (X == other.X &&
                Y == other.Y &&
                Z == other.Z &&
                W == other.W);
    }

    /// <summary>
    /// Returns a boolean indicating whether the given Object is equal to this Quat instance.
    /// </summary>
    /// <param name="obj">The Object to compare against.</param>
    /// <returns>True if the Object is equal to this Quat; False otherwise.</returns>
    public override bool Equals(object obj)
    {
        if (obj is FQuat quat)
        {
            return Equals(quat);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z, W);
    }

    /// <summary>
    /// Returns a String representing this Quat instance.
    /// </summary>
    /// <returns>The string representation.</returns>
    public override string ToString()
    {
        CultureInfo ci = CultureInfo.CurrentCulture;
        return String.Format(ci, "{{X:{0} Y:{1} Z:{2} W:{3}}}", X.ToString(ci), Y.ToString(ci), Z.ToString(ci), W.ToString(ci));
    }
}