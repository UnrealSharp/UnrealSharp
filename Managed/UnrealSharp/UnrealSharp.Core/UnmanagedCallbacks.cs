﻿using System.Reflection;
using System.Runtime.InteropServices;
using UnrealSharp.Core.Attributes;
using UnrealSharp.Core.Marshallers;

namespace UnrealSharp.Core;

public static class UnmanagedCallbacks
{
    [UnmanagedCallersOnly]
    public static unsafe IntPtr CreateNewManagedObject(IntPtr nativeObject, IntPtr typeHandlePtr, char** error)
    {
        try
        {
            if (nativeObject == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(nativeObject));
            }
            
            Type? type = GCHandleUtilities.GetObjectFromHandlePtr<Type>(typeHandlePtr);
            
            if (type == null)
            {
                throw new InvalidOperationException("The provided type handle does not point to a valid type.");
            }

            return UnrealSharpObject.Create(type, nativeObject);
        }
        catch (Exception ex)
        {
            LogUnrealSharpCore.LogError($"Failed to create new managed object: {ex.Message}");
            *error = (char*)Marshal.StringToHGlobalUni(ex.ToString());
        }

        return IntPtr.Zero;
    }

    [UnmanagedCallersOnly]
    public static IntPtr CreateNewManagedObjectWrapper(IntPtr managedObjectHandle, IntPtr typeHandlePtr)
    {
        try
        {
            if (managedObjectHandle == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(managedObjectHandle));
            }
            
            Type? type = GCHandleUtilities.GetObjectFromHandlePtr<Type>(typeHandlePtr);
            
            if (type is null)
            {
                throw new InvalidOperationException("The provided type handle does not point to a valid type.");
            }
            
            object? managedObject = GCHandleUtilities.GetObjectFromHandlePtr<object>(managedObjectHandle);
            if (managedObject is null)
            {
                throw new InvalidOperationException("The provided managed object handle does not point to a valid object.");
            }

            MethodInfo? wrapMethod = type.GetMethod("Wrap", BindingFlags.Public | BindingFlags.Static);
            if (wrapMethod is null)
            {
                throw new InvalidOperationException("The provided type does not have a static Wrap method.");
            }
            
            object? createdObject = wrapMethod.Invoke(null, [managedObject]);
            if (createdObject is null)
            {
                throw new InvalidOperationException("The Wrap method did not return a valid object.");
            }

            return GCHandle.ToIntPtr(GCHandleUtilities.AllocateStrongPointer(createdObject, createdObject.GetType().Assembly));
        }
        catch (Exception ex)
        {
            LogUnrealSharpCore.LogError($"Failed to create new managed object: {ex.Message}");
        }

        return IntPtr.Zero;
    }
    
    [UnmanagedCallersOnly]
    public static unsafe IntPtr LookupManagedMethod(IntPtr typeHandlePtr, char* methodName)
    {
        try
        {
            Type? type = GCHandleUtilities.GetObjectFromHandlePtr<Type>(typeHandlePtr);
            
            if (type == null)
            {
                throw new Exception("Invalid type handle");
            }
            
            string methodNameString = new string(methodName);
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            Type? currentType = type;
            
            while (currentType != null)
            {
                MethodInfo? method = currentType.GetMethod(methodNameString, flags);

                if (method != null)
                {
                    IntPtr functionPtr = method.MethodHandle.GetFunctionPointer();
                    GCHandle methodHandle = GCHandleUtilities.AllocateStrongPointer(functionPtr, type.Assembly);
                    return GCHandle.ToIntPtr(methodHandle);
                }
                
                currentType = currentType.BaseType;
            }

            return IntPtr.Zero;
        }
        catch (Exception e)
        {
            LogUnrealSharpCore.LogError($"Exception while trying to look up managed method: {e.Message}");
        }

        return IntPtr.Zero;
    }
    
    [UnmanagedCallersOnly]
    public static unsafe IntPtr LookupManagedType(IntPtr assemblyHandle, char* fullTypeName)
    {
        try
        {
            string fullTypeNameString = new string(fullTypeName);
            Assembly? loadedAssembly = GCHandleUtilities.GetObjectFromHandlePtr<Assembly>(assemblyHandle);

            if (loadedAssembly == null)
            {
                throw new InvalidOperationException("The provided assembly handle does not point to a valid assembly.");
            }

            return FindTypeInAssembly(loadedAssembly, fullTypeNameString);
        }
        catch (TypeLoadException ex)
        {
            LogUnrealSharpCore.LogError($"TypeLoadException while trying to look up managed type: {ex.Message}");
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
                    return GCHandle.ToIntPtr(GCHandleUtilities.AllocateStrongPointer(type, assembly));
                }
            }
        }

        return IntPtr.Zero;
    }
    
    [UnmanagedCallersOnly]
    public static unsafe int InvokeManagedMethod(IntPtr managedObjectHandle, IntPtr methodHandlePtr, IntPtr exceptionTextBuffer)
    {
        try
        {
            IntPtr? methodHandle = GCHandleUtilities.GetObjectFromHandlePtrFast<IntPtr>(methodHandlePtr);
            object? managedObject = GCHandleUtilities.GetObjectFromHandlePtrFast<object>(managedObjectHandle);
            
            if (methodHandle == null || managedObject == null)
            {
                throw new Exception("Invalid method or target handle");
            }
            
            delegate*<object, void> methodPtr = (delegate*<object, void>) methodHandle;
            methodPtr(managedObject);
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
    public static unsafe int InvokeManagedMethod(IntPtr managedObjectHandle,
        IntPtr methodHandlePtr, 
        IntPtr argumentsBuffer, 
        IntPtr returnValueBuffer, 
        IntPtr exceptionTextBuffer)
    {
        try
        {
            IntPtr? methodHandle = GCHandleUtilities.GetObjectFromHandlePtr<IntPtr>(methodHandlePtr);
            object? managedObject = GCHandleUtilities.GetObjectFromHandlePtr<object>(managedObjectHandle);
            
            if (methodHandle == null || managedObject == null)
            {
                throw new Exception("Invalid method or target handle");
            }
            
            delegate*<object, IntPtr, IntPtr, void> methodPtr = (delegate*<object, IntPtr, IntPtr, void>) methodHandle;
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
            Delegate? foundDelegate = GCHandleUtilities.GetObjectFromHandlePtr<Delegate>(delegatePtr);
            
            if (foundDelegate == null)
            {
                throw new Exception("Invalid delegate handle");
            }

            foundDelegate.DynamicInvoke();
        }
        catch (Exception ex)
        {
            LogUnrealSharpCore.LogError($"Exception during InvokeDelegate: {ex.Message}");
        }
    }

    [UnmanagedCallersOnly]
    public static void Dispose(IntPtr handle, IntPtr assemblyHandle)
    {
        GCHandle foundHandle = GCHandle.FromIntPtr(handle);
        
        if (!foundHandle.IsAllocated)
        {
            return;
        }
        
        if (foundHandle.Target is IDisposable disposable)
        {
            disposable.Dispose();
        }

        Assembly? foundAssembly = GCHandleUtilities.GetObjectFromHandlePtr<Assembly>(assemblyHandle);
        GCHandleUtilities.Free(foundHandle, foundAssembly);
    }

    [UnmanagedCallersOnly]
    public static void FreeHandle(IntPtr handle)
    {
        GCHandle foundHandle = GCHandle.FromIntPtr(handle);
        if (!foundHandle.IsAllocated) return;
        
        if (foundHandle.Target is IDisposable disposable)
        {
            disposable.Dispose();
        }
            
        foundHandle.Free();
    }
}