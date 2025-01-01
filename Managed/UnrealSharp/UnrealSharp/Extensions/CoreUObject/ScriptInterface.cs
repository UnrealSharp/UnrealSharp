using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp.CoreUObject;

[StructLayout(LayoutKind.Sequential)]
internal struct FScriptInterface
{
    public FScriptInterface(IntPtr objectPointer, IntPtr interfacePointer)
    {
        ObjectPointer = objectPointer;
        InterfacePointer = interfacePointer;
    }
    
    internal IntPtr ObjectPointer;
    internal IntPtr InterfacePointer;
}

public static class ScriptInterfaceMarshaller<T> where T : class
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, T obj, IntPtr nativeInterfaceClassPtr)
    {
        unsafe
        {
            FScriptInterface* scriptInterface = (FScriptInterface*)(nativeBuffer + arrayIndex * sizeof(FScriptInterface));
            IntPtr objectPointer = IntPtr.Zero;
            
            if (obj is UObject unrealObject)
            {
                objectPointer = unrealObject.NativeObject;
            }

            scriptInterface->ObjectPointer = objectPointer;
            scriptInterface->InterfacePointer = nativeInterfaceClassPtr;
        }
    }
    
    public static T FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        unsafe
        {
            FScriptInterface* scriptInterface = (FScriptInterface*)(nativeBuffer + arrayIndex * sizeof(FScriptInterface));
            IntPtr handle = FCSManagerExporter.CallFindManagedObject(scriptInterface->ObjectPointer);
            return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
        }
    }
}