using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

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
    
    /// <summary>
    /// Current world context object for this frame.
    /// </summary>
    internal static UObject WorldContextObject
    {
        get
        {
            IntPtr worldContextObject = FCSManagerExporter.CallGetCurrentWorldContext();
            return GcHandleUtilities.GetObjectFromHandlePtr<UObject>(worldContextObject)!;
        }
    }
    
    /// <inheritdoc />
    public virtual void Dispose()
    {
        NativeObject = IntPtr.Zero;
        GC.SuppressFinalize(this);
    }
}

internal static class ReflectionHelper
{
    // Get the name without the U/A/F/E prefix.
    internal static string GetEngineName(this Type type)
    {
        Attribute? generatedTypeAttribute = type.GetCustomAttribute<GeneratedTypeAttribute>();
        
        if (generatedTypeAttribute is null)
        {
            return type.Name;
        }
            
        FieldInfo? field = generatedTypeAttribute.GetType().GetField("EngineName");
        
        if (field == null)
        {
            throw new InvalidOperationException($"The EngineName field was not found in the {nameof(GeneratedTypeAttribute)}.");
        }
            
        return (string) field.GetValue(generatedTypeAttribute)!;
    }
    
    internal static IntPtr TryGetNativeClass(this Type type)
    {
        return UCoreUObjectExporter.CallGetNativeClassFromName(type.GetEngineName());
    }
    
    internal static IntPtr TryGetNativeClassDefaults(this Type type)
    {
        return UClassExporter.CallGetDefaultFromName(type.GetEngineName());
    }
}
