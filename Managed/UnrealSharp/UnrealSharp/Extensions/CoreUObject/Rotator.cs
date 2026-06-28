using UnrealSharp.Interop;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.CoreUObject;

public partial record struct FRotator
{
    /// <summary>
    /// Pitch (degrees) around Y axis
    /// </summary>
    public double Pitch;
    
    /// <summary>
    /// Yaw (degrees) around Z axis
    /// </summary>
    public double Yaw;
    
    /// <summary>
    /// Roll (degrees) around X axis
    /// </summary>
    public double Roll;
    
    public static readonly FRotator ZeroRotator = new(0, 0, 0);

    public FRotator(double pitch, double yaw, double roll)
    {
        Pitch = pitch;
        Yaw = yaw;
        Roll = roll;
    }
    
    public FRotator(FQuat quat)
    {
        UCSQuatExtensions.ToRotator(out this, quat);
    }
    
    public bool Equals(FRotator other)
    {
        return Pitch.Equals(other.Pitch) && Yaw.Equals(other.Yaw) && Roll.Equals(other.Roll);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Pitch, Yaw, Roll);
    }
    
    public FRotator(FMatrix rotationMatrix)
    {
        Bind_FRotator.CallFromMatrix(ref this, rotationMatrix);
    }

    public FRotator(FVector vec)
    {
        Yaw = Math.Atan2(vec.Y, vec.X) * 180.0 / Math.PI;
        Pitch = Math.Atan2(vec.Z, Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y)) * 180.0 / Math.PI;
        Roll = 0.0f;
    }
    
    public FQuat ToQuaternion
    {
        get
        {
            UCSQuatExtensions.ToQuaternion(out FQuat quat, this);
            return quat;
        }
    }
    
    public FMatrix ToMatrix
    {
        get
        {
            Bind_FMatrix.CallFromRotator(out FMatrix rotationMatrix, this);
            return rotationMatrix;
        }
    }
    
    public bool IsZero => Pitch == 0 && Yaw == 0 && Roll == 0;
    
    public bool IsNearlyZero(float tolerance = 0.0001f)
    {
        return Math.Abs(Pitch) < tolerance && Math.Abs(Yaw) < tolerance && Math.Abs(Roll) < tolerance;
    }

    public static FRotator operator + (FRotator lhs, FRotator rhs)
    {
        return new FRotator
        {
            Pitch = lhs.Pitch + rhs.Pitch,
            Yaw = lhs.Yaw + rhs.Yaw,
            Roll = lhs.Roll + rhs.Roll
        };
    }

    public static FRotator operator - (FRotator lhs, FRotator rhs)
    {
        return new FRotator
        {
            Pitch = lhs.Pitch - rhs.Pitch,
            Yaw = lhs.Yaw - rhs.Yaw,
            Roll = lhs.Roll - rhs.Roll
        };
    }

    public static FRotator operator -(FRotator rotator)
    {
        return new FRotator
        {
            Pitch = -rotator.Pitch,
            Yaw = -rotator.Yaw,
            Roll = -rotator.Roll
        };
    }

    public static FRotator operator *(FRotator rotator, double scale)
    {
        return new FRotator
        {
            Pitch = rotator.Pitch * scale,
             Yaw = rotator.Yaw * scale,
            Roll = rotator.Roll * scale
        };
    }
    
    public static FRotator operator *(FRotator rotator, float scale)
    {
        return new FRotator
        {
            Pitch = rotator.Pitch * scale,
            Yaw = rotator.Yaw * scale,
            Roll = rotator.Roll * scale
        };
    }

    public static FRotator operator *(double scale, FRotator rotator)
    {
        return rotator * scale;
    }
    
    public static FRotator operator *(float scale, FRotator rotator)
    {
        return rotator * scale;
    }
    
    public static implicit operator FRotator(FQuat quat) => new FRotator(quat);
    public static implicit operator FRotator(FMatrix matrix) => new FRotator(matrix);
    public static implicit operator FRotator(FVector vector) => new FRotator(vector);
}