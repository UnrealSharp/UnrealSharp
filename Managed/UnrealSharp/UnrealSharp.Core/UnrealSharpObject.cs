using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnrealSharp.Core;

/// <summary>
/// Represents a UObject in Unreal Engine. Don't inherit from this class directly, use a CoreUObject.Object instead.
/// </summary>
public class UnrealSharpObject : IDisposable
{
    internal static unsafe IntPtr Create(Type typeToCreate, IntPtr nativeObjectPtr)
    {
        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        ConstructorInfo? foundDefaultCtor = typeToCreate.GetConstructor(bindingFlags, Type.EmptyTypes);
            
        if (foundDefaultCtor == null)
        {
            LogUnrealSharpCore.LogError("Failed to find default constructor for type: " + typeToCreate.FullName);
            return IntPtr.Zero;
        }
            
        delegate*<object, void> foundConstructor = (delegate*<object, void>) foundDefaultCtor.MethodHandle.GetFunctionPointer();
            
        UnrealSharpObject createdObject = (UnrealSharpObject) RuntimeHelpers.GetUninitializedObject(typeToCreate);
        createdObject.NativeObject = nativeObjectPtr;
            
        foundConstructor(createdObject);
            
        return GCHandle.ToIntPtr(GCHandleUtilities.AllocateStrongPointer(createdObject, typeToCreate.Assembly));
    }
    
    /// <summary>
    /// The pointer to the UObject that this C# object represents.
    /// </summary>
    public IntPtr NativeObject { get; private set; }
    
    /// <summary>
    /// Current world context object for this frame.
    /// </summary>
    internal static UnrealSharpObject WorldContextObject
    {
        get
        {
            IntPtr worldContextObject = FCSManagerExporter.CallGetCurrentWorldContext();
            IntPtr handle = FCSManagerExporter.CallFindManagedObject(worldContextObject);
            return GCHandleUtilities.GetObjectFromHandlePtr<UnrealSharpObject>(handle)!;
        }
    }
    
    /// <inheritdoc />
    public virtual void Dispose()
    {
        NativeObject = IntPtr.Zero;
        GC.SuppressFinalize(this);
    }
}
