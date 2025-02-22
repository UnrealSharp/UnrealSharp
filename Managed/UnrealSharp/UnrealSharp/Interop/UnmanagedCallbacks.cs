using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using UnrealSharp.Attributes;
using UnrealSharp.Logging;

namespace UnrealSharp.Interop;

public static class UnmanagedCallbacks
{
    [UnmanagedCallersOnly]
    internal static IntPtr CreateNewManagedObject(IntPtr nativeObject, IntPtr typeHandlePtr)
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
            LogUnrealSharp.Log($"Failed to create new managed object: {ex.Message}");
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
                    GCHandle methodHandle = GcHandleUtilities.AllocateStrongPointer(method, type.Assembly);
                    return GCHandle.ToIntPtr(methodHandle);
                }
                
                currentType = currentType.BaseType;
            }

            return default;
        }
        catch (Exception e)
        {
            LogUnrealSharp.LogError($"Exception while trying to look up managed method: {e.Message}");
        }

        return default;
    }

    [UnmanagedCallersOnly]
    public static unsafe IntPtr LookupManagedType(IntPtr assemblyHandle, char* fullTypeName)
    {
        try
        {
            string fullTypeNameString = new string(fullTypeName);
            GCHandle handle = GCHandle.FromIntPtr(assemblyHandle);
            Assembly? loadedAssembly = handle.Target as Assembly;

            if (loadedAssembly == null)
            {
                throw new InvalidOperationException("The provided assembly handle does not point to a valid assembly.");
            }

            return FindTypeInAssembly(loadedAssembly, fullTypeNameString);
        }
        catch (TypeLoadException ex)
        {
            LogUnrealSharp.LogError($"TypeLoadException while trying to look up managed type: {ex.Message}");
            return IntPtr.Zero;
        }
    }
    
    private static IntPtr FindTypeInAssembly(Assembly assembly, string fullTypeName)
    {
        Type[] types = assembly.GetTypes();
        foreach (Type type in types)
        {
            foreach (CustomAttributeData attributeData in type.CustomAttributes)
            {
                if (attributeData.AttributeType.FullName != typeof(GeneratedTypeAttribute).FullName)
                {
                    continue;
                }

                if (attributeData.ConstructorArguments.Count != 2)
                {
                    continue;
                }

                string fullName = (string)attributeData.ConstructorArguments[1].Value!;
                if (fullName == fullTypeName)
                {
                    return GCHandle.ToIntPtr(GcHandleUtilities.AllocateStrongPointer(type, assembly));
                }
            }
        }

        return IntPtr.Zero;
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
            LogUnrealSharp.LogError($"Exception during InvokeManagedMethod: {ex.Message}");
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
            LogUnrealSharp.LogError($"Exception during InvokeDelegate: {ex.Message}");
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

        GcHandleUtilities.Free(foundHandle);
    }
}