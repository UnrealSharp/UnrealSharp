using UnrealSharp.Interop;

namespace UnrealSharp.CoreUObject;

public partial struct Rotator
{
    public bool Equals(Rotator other)
    {
        return Pitch.Equals(other.Pitch) && Yaw.Equals(other.Yaw) && Roll.Equals(other.Roll);
    }

    public override bool Equals(object obj)
    {
        return obj is Rotator other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Pitch, Yaw, Roll);
    }

    public static readonly Rotator ZeroRotator = new(0, 0, 0);

    public Rotator(double pitch, double yaw, double roll)
    {
        Pitch = pitch;
        Yaw = yaw;
        Roll = roll;
    }

    public Rotator(Quat quat)
    {
        FRotatorExporter.CallFromQuat(out this, ref quat);
    }
    
    public Rotator(Matrix rotationMatrix)
    {
        FRotatorExporter.CallFromMatrix(out this, ref rotationMatrix);
    }

    public Rotator(Vector vec)
    {
        Yaw = Math.Atan2(vec.Y, vec.X) * 180.0 / Math.PI;
        Pitch = Math.Atan2(vec.Z, Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y)) * 180.0 / Math.PI;
        Roll = 0.0f;
    }
    
    public Quat ToQuaternion()
    {
        FQuatExporter.CallToQuaternion(out var quat, ref this);
        return quat;
    }

    public Matrix ToMatrix()
    {
        FMatrixExporter.CallFromRotator(out var rotationMatrix, ref this);
        return rotationMatrix;
    }

    // Convert the rotator into a vector facing in its direction.
    public Vector ToVector()
    {
        return FVectorExporter.CallFromRotator(out this);
    }

    public static Rotator operator + (Rotator lhs, Rotator rhs)
    {
        return new Rotator
        {
            Pitch = lhs.Pitch + rhs.Pitch,
            Yaw = lhs.Yaw + rhs.Yaw,
            Roll = lhs.Roll + rhs.Roll
        };
    }

    public static Rotator operator - (Rotator lhs, Rotator rhs)
    {
        return new Rotator
        {
            Pitch = lhs.Pitch - rhs.Pitch,
            Yaw = lhs.Yaw - rhs.Yaw,
            Roll = lhs.Roll - rhs.Roll
        };
    }

    public static Rotator operator -(Rotator rotator)
    {
        return new Rotator
        {
            Pitch = -rotator.Pitch,
            Yaw = -rotator.Yaw,
            Roll = -rotator.Roll
        };
    }

    public static Rotator operator *(Rotator rotator, double scale)
    {
        return new Rotator
        {
            Pitch = rotator.Pitch * scale,
            Yaw = rotator.Yaw * scale,
            Roll = rotator.Roll * scale
        };
    }

    public static Rotator operator *(double scale, Rotator rotator)
    {
        return rotator * scale;
    }
    
    public static bool operator == (Rotator left, Rotator right)
    {
        float tolerance = 0.0001f;

        return Math.Abs(left.Pitch - right.Pitch) < tolerance &&
               Math.Abs(left.Roll - right.Roll) < tolerance &&
               Math.Abs(left.Yaw - right.Yaw) < tolerance;
    }
    public static bool operator !=(Rotator left, Rotator right)
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