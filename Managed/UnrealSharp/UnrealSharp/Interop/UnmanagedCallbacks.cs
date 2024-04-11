using System.Reflection;
using System.Runtime.InteropServices;

namespace UnrealSharp.Interop;

public static class UnmanagedCallbacks
{
    [UnmanagedCallersOnly]
    internal static IntPtr CreateNewManagedObject(IntPtr nativeObject, IntPtr typeHandle)
    {
        try
        {
            if (nativeObject == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(nativeObject));
            }
            
            RuntimeTypeHandle handle = RuntimeTypeHandle.FromIntPtr(typeHandle);

            if (handle == null)
            {
                throw new ArgumentNullException(nameof(nativeObject));
            }

            Type? typeToCreate = Type.GetTypeFromHandle(handle);

            if (typeToCreate == null)
            {
                throw new ArgumentNullException(nameof(nativeObject));
            }

            return UnrealSharpObject.Create(typeToCreate, nativeObject);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create new managed object: {ex.Message}");
        }

        return default;
    }
    
    [UnmanagedCallersOnly]
    public static unsafe IntPtr LookupManagedMethod(IntPtr typeHandlePtr, char* methodName)
    {
        try
        {
            string methodNameString = new string(methodName);
            RuntimeTypeHandle typeHandle = RuntimeTypeHandle.FromIntPtr(typeHandlePtr);
            Type? foundType = Type.GetTypeFromHandle(typeHandle);

            if (typeHandle == null || foundType == null)
            {
                throw new Exception("Couldn't find type with TypeHandle");
            }
            
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var currentType = foundType;
            
            while (currentType != null)
            {
                MethodInfo? method = currentType.GetMethod(methodNameString, flags);

                if (method != null)
                {
                    return method.MethodHandle.GetFunctionPointer();
                }
                
                currentType = currentType.BaseType;
            }

            return default;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception while trying to look up managed method: {e.Message}");
        }

        return default;
    }

    [UnmanagedCallersOnly]
    public static unsafe IntPtr LookupManagedType(IntPtr assemblyHandle, char* typeNamespace, char* typeName)
    {
        try
        {
            Assembly? loadedAssembly = GCHandle.FromIntPtr(assemblyHandle).Target as Assembly;

            if (loadedAssembly == null)
            {
                throw new InvalidOperationException("The provided assembly handle does not point to a valid assembly.");
            }

            string typeNamespaceString = new string(typeNamespace);
            string typeNameString = new string(typeName);

            string fullTypeName = $"{typeNamespaceString}.{typeNameString}";

            Type? foundType = loadedAssembly.GetType(fullTypeName, false);

            if (foundType != null)
            {
                return foundType.TypeHandle.Value;
            }

            foundType = typeof(UnrealSharpObject).Assembly.GetType(fullTypeName, true);

            if (foundType == null)
            {
                throw new Exception($"The type '{fullTypeName}' was not found in {loadedAssembly}.");
            }

            return foundType.TypeHandle.Value;
        }
        catch (TypeLoadException ex)
        {
            Console.WriteLine($"Exception while trying to look up managed type: {ex.Message}, {ex.StackTrace}, {ex.InnerException}");
            return default;
        }
    }
        
    [UnmanagedCallersOnly]
    public static unsafe int InvokeManagedMethod(IntPtr managedObjectHandle,
        delegate*<object, IntPtr, IntPtr, void> methodPtr, 
        IntPtr argumentsBuffer, 
        IntPtr returnValueBuffer, 
        IntPtr exceptionTextBuffer)
    {
        try
        {
            object? managedObject = GCHandle.FromIntPtr(managedObjectHandle).Target;
            
            if (managedObject == null)
            {
                throw new ArgumentNullException(nameof(managedObject));
            }
            
            methodPtr(managedObject, argumentsBuffer, returnValueBuffer);
        }
        catch (Exception ex)
        {
            StringMarshaller.ToNative(exceptionTextBuffer, 0, null, ex.ToString());
            Console.WriteLine($"Exception during InvokeManagedMethod: {ex}");
            return 1;
        }
        return 0;
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
            Console.WriteLine($"Exception during InvokeDelegate: {ex}");
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