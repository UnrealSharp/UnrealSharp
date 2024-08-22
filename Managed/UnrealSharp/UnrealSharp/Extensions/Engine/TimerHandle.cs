using System.Runtime.InteropServices;
using UnrealSharp.Attributes;

namespace UnrealSharp.Engine;

[UStruct, BlittableType, StructLayout(LayoutKind.Sequential)]
public partial struct FTimerHandle
{
    private const uint IndexBits = 24;
    private const uint SerialNumberBits = 40;
    
    private const int MaxIndex = 1 << (int)IndexBits;
    private const ulong MaxSerialNumber = 1UL << (int)SerialNumberBits;
    
    private ulong Handle;

    public FTimerHandle()
    {
        Handle = 0;
    }

    public bool IsValid()
    {
        return Handle != 0;
    }

    public override bool Equals(object? obj)
    {
        if (obj is FTimerHandle other)
        {
            return Handle == other.Handle;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Handle.GetHashCode();
    }

    public static bool operator ==(FTimerHandle left, FTimerHandle right)
    {
        return left.Handle == right.Handle;
    }

    public static bool operator !=(FTimerHandle left, FTimerHandle right)
    {
        return left.Handle != right.Handle;
    }
    public override string ToString()
    {
        return Handle.ToString();
    }
}