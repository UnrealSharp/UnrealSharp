using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnrealSharp.Core.Interop;

namespace UnrealSharp.Core;

[Serializable]
public class UnrealObjectDestroyedException : InvalidOperationException
{
    public UnrealObjectDestroyedException()
    {

    }

    public UnrealObjectDestroyedException(string message)
        : base(message)
    {

    }

    public UnrealObjectDestroyedException(string message, Exception innerException)
        : base(message, innerException)
    {

    }
}

public class UnrealSharpObject : IDisposable
{
    public IntPtr NativeObject { get; private set; }
    public Name ObjectName => IsDestroyed ? Name.None : UObjectExporter.CallNativeGetName(NativeObject);
    public bool IsDestroyed => NativeObject == IntPtr.Zero;
    
    internal static IntPtr Create(Type typeToCreate, IntPtr nativeObjectPtr)
    {
        UnrealSharpObject createdObject = (UnrealSharpObject) RuntimeHelpers.GetUninitializedObject(typeToCreate);
        
        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var foundConstructor = typeToCreate.GetConstructor(bindingFlags, null, Type.EmptyTypes, null);
        
        createdObject.NativeObject = nativeObjectPtr;

        foundConstructor.Invoke(createdObject, null);
        
        return GCHandle.ToIntPtr(GcHandleUtilities.AllocateStrongPointer(createdObject));
    }

    public override string ToString()
    {
        return ObjectName.ToString();
    }

    public override bool Equals(object obj)
    {
        return obj is UnrealSharpObject unrealSharpObject && NativeObject == unrealSharpObject.NativeObject;
    }

    public override int GetHashCode()
    {
        return NativeObject.GetHashCode();
    }
    
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
