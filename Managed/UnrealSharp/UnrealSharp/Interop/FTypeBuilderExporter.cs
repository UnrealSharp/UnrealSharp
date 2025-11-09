using System.Runtime.InteropServices;
using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FTypeBuilderExporter
{
    public static delegate* unmanaged<string, string, string, string, byte, IntPtr, void> NewType_Internal;
    
    public static void NewType(string typeName, string jsonString, byte fieldType, Type type)
    {
        IntPtr handlePtr = GCHandle.ToIntPtr(GCHandleUtilities.AllocateStrongPointer(type, type.Assembly));
        NewType_Internal(typeName, type.Namespace!, type.Assembly.GetName().Name!, jsonString, fieldType, handlePtr);
    }
}
