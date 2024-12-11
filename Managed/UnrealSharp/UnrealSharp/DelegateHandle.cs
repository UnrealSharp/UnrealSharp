using System.Runtime.InteropServices;

namespace UnrealSharp;

/// <summary>
/// Struct representing a handle to a delegate.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct FDelegateHandle : IEquatable<FDelegateHandle>
{
    public ulong ID;

    public void Reset()
    {
        ID = 0;
    }

    public static bool operator ==(FDelegateHandle a, FDelegateHandle b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(FDelegateHandle a, FDelegateHandle b)
    {
        return !a.Equals(b);
    }

    public override bool Equals(object obj)
    {
        return obj is FDelegateHandle handle && Equals(handle);
    }

    public bool Equals(FDelegateHandle other)
    {
        return ID == other.ID;
    }
    
    public override int GetHashCode()
    {
        return ID.GetHashCode();
    }
}