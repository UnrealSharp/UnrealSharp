using System.Runtime.InteropServices;
using UnrealSharp.Binds;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class Bind_GEditor
{
    public static delegate* unmanaged<IntPtr, IntPtr> GetEditorSubsystem;

    public static void Test(IntPtr test)
    {
        GetEditorSubsystem = (delegate* unmanaged<IntPtr, IntPtr>)test;
    }
}