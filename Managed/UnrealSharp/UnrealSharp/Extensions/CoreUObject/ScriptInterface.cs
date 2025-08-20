using System.Diagnostics.CodeAnalysis;
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

public static class ScriptInterfaceExtensions 
{
    /// <summary>
    /// Attempt to cast a UObject to an interface.
    /// </summary>
    /// <param name="uobject">The source UObject.</param>
    /// <typeparam name="T">The interface type</typeparam>
    /// <returns>The cast interface, or null if the conversion could not be made.</returns>
    public static T? AsInterface<T>(this UObject? uobject) where T : class
    {
        switch (uobject)
        {
            case null:
                return null;
            case T typedObj:
                return typedObj;
        }

        var nativeClass = typeof(T).TryGetNativeInterface();
        if (nativeClass == IntPtr.Zero)
        {
            return null;
        }
            
        var wrapperHandle = FCSManagerExporter.CallFindOrCreateManagedInterfaceWrapper(uobject.NativeObject, nativeClass);
		if(wrapperHandle == IntPtr.Zero)
        {
            return null;
        }
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

    /// <summary>
    /// Attempt to cast a UObject to an interface.
    /// </summary>
    /// <param name="uobject">The source UObject.</param>
    /// <typeparam name="T">The interface type</typeparam>
    /// <returns>The cast interface, or null if the object was null.</returns>
    /// <exception cref="InvalidCastException">If a non-null object could not be converted</exception>
    [return: NotNullIfNotNull(nameof(uobject))]
    public static T? CastInterface<T>(this UObject? uobject) where T : class
    {
        if (uobject is null)
        {
            return null;
        }
        
        return uobject.AsInterface<T>() ?? throw new InvalidCastException($"Cannot cast {uobject.GetType()} to {typeof(T)}");
    }
    
    /// <summary>
    /// Attempt to convert an interface object to a UObject.
    /// </summary>
    /// <param name="scriptInterface">The interface object</param>
    /// <returns>The cast UObject or null if the conversion could not be made.</returns>
    public static UObject? AsUObject(this object? scriptInterface)
    {
        return scriptInterface switch
        {
            UObject uobject => uobject,
            IScriptInterface scriptInterfaceObject => scriptInterfaceObject.Object,
            _ => null
        };

    }

    /// <summary>
    /// Attempt to convert an interface object to a UObject.
    /// </summary>
    /// <param name="scriptInterface">The interface object</param>
    /// <returns>The cast UObject or null if the argument is null.</returns>
    /// <exception cref="InvalidCastException">If a non-null object could not be converted</exception>
    [return: NotNullIfNotNull(nameof(scriptInterface))]
    public static UObject? CastUObject(this object? scriptInterface)
    {
        if (scriptInterface is null)
        {
            return null;
        }
        
        return scriptInterface.AsUObject() ?? throw new InvalidCastException($"Cannot cast {scriptInterface.GetType()} to UObject");
    }
}



public static class ScriptInterfaceMarshaller<T> where T : class
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, T obj)
    {
        unsafe
        {
            FScriptInterface* scriptInterface = (FScriptInterface*)(nativeBuffer + arrayIndex * sizeof(FScriptInterface));
            IntPtr objectPointer = obj.AsUObject()?.NativeObject ?? IntPtr.Zero;
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
            return uobject.AsInterface<T>();
        }
    }
}
