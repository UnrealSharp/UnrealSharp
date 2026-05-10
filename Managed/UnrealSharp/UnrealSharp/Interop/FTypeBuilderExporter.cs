using System.Runtime.InteropServices;
using UnrealSharp.Binds;
using UnrealSharp.Core;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FTypeBuilderExporter
{
    public static delegate* unmanaged<char*, char*, char*, char*, byte, IntPtr, void> RegisterManagedType_Native;
    
    public static void RegisterManagedType(string typeName, string jsonString, byte fieldType, Type type)
    {
        IntPtr handlePtr = GCHandle.ToIntPtr(GCHandleUtilities.AllocateStrongPointer(type, type.Assembly));
        
        fixed (char* nTypeName = typeName)
        fixed (char* nNamespace = type.Namespace)
        fixed (char* nAssemblyName = type.Assembly.GetName().Name)
        fixed (char* nJson = jsonString)
        {
            RegisterManagedType_Native(nTypeName, nNamespace, nAssemblyName, nJson, fieldType, handlePtr);
        }
    }
}
