using UnrealSharp.Core.Attributes;

namespace UnrealSharp.Engine.Interop;

[NativeCallbacks]
public static unsafe partial class AActorExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr> GetInputComponent;
}