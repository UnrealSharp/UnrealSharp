using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UnrealSharp.CoreUObject;

public partial struct FGuid : IEquatable<FGuid>
{
    public FGuid(Guid guid)
    {
        ReadOnlySpan<int> span = MemoryMarshal.Cast<Guid, int>(new ReadOnlySpan<Guid>(in guid));

        A = span[0];
        B = span[1];
        C = span[2];
        D = span[3];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Guid GetGuid() => new(MemoryMarshal.Cast<int, byte>([A, B, C, D]));

    public static implicit operator Guid(FGuid guid)
    {
        return guid.GetGuid();
    }

    public static implicit operator FGuid(Guid guid)
    {
        return new FGuid(guid);
    }

    public override bool Equals(object? obj)
    {
        return obj is FGuid guid && Equals(guid);
    }

    public bool Equals(FGuid other)
    {
        return A == other.A &&
               B == other.B &&
               C == other.C &&
               D == other.D;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(A, B, C, D);
    }

    public static bool operator ==(FGuid left, FGuid right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FGuid left, FGuid right)
    {
        return !(left == right);
    }
}
