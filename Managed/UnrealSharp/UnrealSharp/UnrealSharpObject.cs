using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnrealSharp;

/// <summary>
/// Represents a UObject in Unreal Engine. Don't inherit from this class directly, use a CoreUObject.Object instead.
/// </summary>
public class UnrealSharpObject : IDisposable
{
    internal static IntPtr Create(Type typeToCreate, IntPtr nativeObjectPtr)
    {
        unsafe
        {
            UnrealSharpObject createdObject = (UnrealSharpObject) RuntimeHelpers.GetUninitializedObject(typeToCreate);
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var foundConstructor = (delegate*<object, void>) typeToCreate.GetConstructor(bindingFlags, Type.EmptyTypes)!.MethodHandle.GetFunctionPointer();
            createdObject.NativeObject = nativeObjectPtr;
            foundConstructor(createdObject);
            return GCHandle.ToIntPtr(GcHandleUtilities.AllocateStrongPointer(createdObject));
        }
    }
    
    /// <summary>
    /// The pointer to the UObject that this C# object represents.
    /// </summary>
    public IntPtr NativeObject { get; private set; }
    
    /// <inheritdoc />
    public virtual void Dispose()
    {
        NativeObject = IntPtr.Zero;
        GC.SuppressFinalize(this);
    }
}
