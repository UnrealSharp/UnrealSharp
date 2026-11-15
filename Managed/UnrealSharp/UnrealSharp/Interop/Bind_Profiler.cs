using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_Profiler
{
    public static delegate* unmanaged<byte*, long> BeginCpuTraceScope;
    public static delegate* unmanaged<long, byte> EndCpuTraceScope;
}
