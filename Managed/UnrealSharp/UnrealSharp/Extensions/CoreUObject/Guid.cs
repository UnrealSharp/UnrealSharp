using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UnrealSharp.CoreUObject;

public partial struct FGuid
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
}
