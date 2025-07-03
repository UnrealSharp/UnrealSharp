using System.Runtime.InteropServices;
using UnrealSharp.Core;
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

public interface IScriptInterface
{
    public UObject Object { get; }
    
}



public static class ScriptInterfaceMarshaller<T> where T : class
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, T obj)
    {
        unsafe
        {
            FScriptInterface* scriptInterface = (FScriptInterface*)(nativeBuffer + arrayIndex * sizeof(FScriptInterface));
            IntPtr objectPointer = IntPtr.Zero;
            
            if (obj is UObject unrealObject)
            {
                objectPointer = unrealObject.NativeObject;
            }
            else if (obj is IScriptInterface scriptInterfaceObject)
            {
                objectPointer = scriptInterfaceObject.Object.NativeObject;
            }

            scriptInterface->ObjectPointer = objectPointer;
            scriptInterface->InterfacePointer = objectPointer;
        }
    }
    
    public static T? FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        unsafe
        {
            FScriptInterface* scriptInterface = (FScriptInterface*)(nativeBuffer + arrayIndex * sizeof(FScriptInterface));
            IntPtr handle = FCSManagerExporter.CallFindManagedObject(scriptInterface->ObjectPointer);
            var uobject = GCHandleUtilities.GetObjectFromHandlePtr<UObject>(handle);
            if (uobject == null)
            {
                return null;
            }

            var nativeClass = typeof(T).TryGetNativeInterface();
            if (nativeClass == IntPtr.Zero)
            {
                return null;
            }
            
            var wrapperHandle = FCSManagerExporter.CallFindOrCreateManagedInterfaceWrapper(uobject.NativeObject, nativeClass);
            GCHandle wrapperGcHandle = GCHandle.FromIntPtr(wrapperHandle);
            if (!wrapperGcHandle.IsAllocated)
            {
                return null;
            }

            if (wrapperGcHandle.Target is T typedWrapper)
            {
                return typedWrapper;
            }
			
			return null;
        }
    }
}