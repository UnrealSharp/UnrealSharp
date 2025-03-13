using System.Reflection;
using System.Runtime.InteropServices;
using UnrealSharp.Core.Marshallers;

namespace UnrealSharp.Core;

public static class UnmanagedCallbacks
{
    [UnmanagedCallersOnly]
    public static IntPtr CreateNewManagedObject(IntPtr nativeObject, IntPtr typeHandlePtr)
    {
        try
        {
            if (nativeObject == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(nativeObject));
            }
            
            GCHandle handle = GCHandle.FromIntPtr(typeHandlePtr);
            
            if (handle.Target is not Type type)
            {
                throw new ArgumentNullException(nameof(typeHandlePtr));
            }

            return UnrealSharpObject.Create(type, nativeObject);
        }
        catch (Exception ex)
        {
            LogUnrealSharpCore.Log($"Failed to create new managed object: {ex.Message}");
        }

        return default;
    }
    
    [UnmanagedCallersOnly]
    public static unsafe IntPtr LookupManagedMethod(IntPtr typeHandlePtr, char* methodName)
    {
        try
        {
            string methodNameString = new string(methodName);
            GCHandle typeHandle = GCHandle.FromIntPtr(typeHandlePtr);
            
            if (typeHandle.Target is not Type type)
            {
                throw new Exception("Invalid type handle");
            }
            
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var currentType = type;
            
            while (currentType != null)
            {
                MethodInfo? method = currentType.GetMethod(methodNameString, flags);

                if (method != null)
                {
                    GCHandle methodHandle = GCHandleUtilities.AllocateStrongPointer(method, type.Assembly);
                    return GCHandle.ToIntPtr(methodHandle);
                }
                
                currentType = currentType.BaseType;
            }

            return default;
        }
        catch (Exception e)
        {
            LogUnrealSharpCore.LogError($"Exception while trying to look up managed method: {e.Message}");
        }

        return default;
    }
    
    [UnmanagedCallersOnly]
    public static unsafe int InvokeManagedMethod(IntPtr managedObjectHandle,
        IntPtr methodHandle, 
        IntPtr argumentsBuffer, 
        IntPtr returnValueBuffer, 
        IntPtr exceptionTextBuffer)
    {
        try
        {
            GCHandle handle = GCHandle.FromIntPtr(methodHandle);
            
            if (handle.Target is not MethodInfo methodInfo)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }
            
            object? managedObject = GCHandle.FromIntPtr(managedObjectHandle).Target;
            
            if (managedObject == null)
            {
                throw new ArgumentNullException(nameof(managedObject));
            }

            delegate*<object, IntPtr, IntPtr, void> methodPtr = (delegate*<object, IntPtr, IntPtr, void>) methodInfo.MethodHandle.GetFunctionPointer();
            methodPtr(managedObject, argumentsBuffer, returnValueBuffer);
            return 0;
        }
        catch (Exception ex)
        {
            StringMarshaller.ToNative(exceptionTextBuffer, 0, ex.ToString());
            LogUnrealSharpCore.LogError($"Exception during InvokeManagedMethod: {ex.Message}");
            return 1;
        }
    }

    [UnmanagedCallersOnly]
    public static void InvokeDelegate(IntPtr delegatePtr)
    {
        try
        {
            if (delegatePtr == IntPtr.Zero)
            {
                return;
            }

            GCHandle foundHandle = GCHandle.FromIntPtr(delegatePtr);

            if (foundHandle.Target is not Delegate @delegate)
            {
                return;
            }

            @delegate.DynamicInvoke();
        }
        catch (Exception ex)
        {
            LogUnrealSharpCore.LogError($"Exception during InvokeDelegate: {ex.Message}");
        }
    }

    [UnmanagedCallersOnly]
    public static void Dispose(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }

        GCHandle foundHandle = GCHandle.FromIntPtr(handle);

        if (foundHandle.Target is IDisposable disposable)
        {
            disposable.Dispose();
        }

        GCHandleUtilities.Free(foundHandle);
    }
}