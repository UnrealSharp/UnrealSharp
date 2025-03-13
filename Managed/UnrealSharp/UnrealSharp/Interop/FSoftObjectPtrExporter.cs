using UnrealSharp.Binds;
using UnrealSharp.EnhancedInput;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FSoftObjectPtrExporter
{
    public static delegate* unmanaged<ref FPersistentObjectPtrData<FSoftObjectPathUnsafe>, IntPtr> LoadSynchronous;
    
    public static void CallBindAction(nint a, nint b, UnrealSharp.EnhancedInput.ETriggerEvent c, nint d, UnrealSharp.FName e)
    {
        if (UEnhancedInputComponentExporter.BindAction == null)
        {
            int totalSize = 0;
            totalSize += sizeof(nint);
            totalSize += sizeof(nint);
            totalSize += sizeof(UnrealSharp.EnhancedInput.ETriggerEvent);
            totalSize += sizeof(nint);
            totalSize += sizeof(UnrealSharp.FName);
            IntPtr funcPtr = NativeBinds.TryGetBoundFunction("UEnhancedInputComponent::BindAction", "BindAction", totalSize);
            UEnhancedInputComponentExporter.BindAction = (delegate* unmanaged<IntPtr, IntPtr, ETriggerEvent, IntPtr, FName, void>)funcPtr;
        }

        UEnhancedInputComponentExporter.BindAction(a, b, c, d, e);
    }
}