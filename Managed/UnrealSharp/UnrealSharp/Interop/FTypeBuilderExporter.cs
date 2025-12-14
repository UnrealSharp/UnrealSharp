using System.Runtime.InteropServices;
using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FTypeBuilderExporter
{
    public static delegate* unmanaged<string, string, string, string, byte, IntPtr, void> RegisterManagedType_Native;
    
    public static void RegisterManagedType(string typeName, string jsonString, byte fieldType, Type type)
    {
        IntPtr handlePtr = GCHandle.ToIntPtr(GCHandleUtilities.AllocateStrongPointer(type, type.Assembly));
        RegisterManagedType_Native(typeName, type.Namespace!, type.Assembly.GetName().Name!, jsonString, fieldType, handlePtr);
    }
}
