using UnrealSharp.Binds;

namespace UnrealSharp.Core.Interop;

[NativeCallbacks]
public static unsafe partial class FOptionalPropertyExporter
{
    public static delegate* unmanaged<IntPtr, IntPtr, NativeBool> IsSet;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> MarkSetAndGetInitializedValuePointerToReplace;
    public static delegate* unmanaged<IntPtr, IntPtr, void> MarkUnset;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> GetValuePointerForRead;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> GetValuePointerForReplace;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> GetValuePointerForReadIfSet;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> GetValuePointerForReplaceIfSet;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> GetValuePointerForReadOrReplace;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> GetValuePointerForReadOrReplaceIfSet;
    public static delegate* unmanaged<IntPtr, int> CalcSize;
}