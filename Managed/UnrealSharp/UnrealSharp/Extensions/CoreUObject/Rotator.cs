using UnrealSharp.Interop;

namespace UnrealSharp.CoreUObject;

public partial struct FRotator
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
    
    public bool Equals(FRotator other)
    {
        return Pitch.Equals(other.Pitch) && Yaw.Equals(other.Yaw) && Roll.Equals(other.Roll);
    }

    public override bool Equals(object? obj)
    {
        return obj is FRotator other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Pitch, Yaw, Roll);
    }

    public static readonly FRotator ZeroRotator = new(0, 0, 0);

    public FRotator(double pitch, double yaw, double roll)
    {
        Pitch = pitch;
        Yaw = yaw;
        Roll = roll;
    }

    public FRotator(FQuat quat)
    {
        FRotatorExporter.CallFromQuat(out this, ref quat);
    }
    
    public FRotator(FMatrix rotationMatrix)
    {
        FRotatorExporter.CallFromMatrix(out this, ref rotationMatrix);
    }

    public FRotator(FVector vec)
    {
        Yaw = Math.Atan2(vec.Y, vec.X) * 180.0 / Math.PI;
        Pitch = Math.Atan2(vec.Z, Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y)) * 180.0 / Math.PI;
        Roll = 0.0f;
    }
    
    public FQuat ToQuaternion()
    {
        FQuatExporter.CallToQuaternion(out var quat, ref this);
        return quat;
    }

    public FMatrix ToMatrix()
    {
        FMatrixExporter.CallFromRotator(out var rotationMatrix, ref this);
        return rotationMatrix;
    }

    // Convert the rotator into a vector facing in its direction.
    public FVector ToVector()
    {
        return FVectorExporter.CallFromRotator(out this);
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

    public static FRotator operator *(double scale, FRotator rotator)
    {
        return rotator * scale;
    }
    
    public static bool operator == (FRotator left, FRotator right)
    {
        float tolerance = 0.0001f;

        return Math.Abs(left.Pitch - right.Pitch) < tolerance &&
               Math.Abs(left.Roll - right.Roll) < tolerance &&
               Math.Abs(left.Yaw - right.Yaw) < tolerance;
    }
    public static bool operator !=(FRotator left, FRotator right)
    {
        return !(left == right);
    }
    
    public bool IsZero()
    {
        
        return Pitch == 0 && Yaw == 0 && Roll == 0;
    }
    
    public bool IsNearlyZero(float tolerance = 0.0001f)
    {
        return Math.Abs(Pitch) < tolerance && Math.Abs(Yaw) < tolerance && Math.Abs(Roll) < tolerance;
    }
    
    public override string ToString()
    {
        return $"({Pitch}, {Yaw}, {Roll})";
    }
}