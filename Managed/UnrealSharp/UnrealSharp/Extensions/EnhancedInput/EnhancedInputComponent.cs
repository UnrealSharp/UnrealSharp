using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp.EnhancedInput;

public partial class UEnhancedInputComponent
{
    public bool BindAction(UInputAction action, ETriggerEvent triggerEvent, Action<FInputActionValue, float, float, UInputAction> callback, out uint handle)
    {
        if (callback.Target is not UObject unrealObject)
        {
            throw new ArgumentException("The callback must be a method within a UObject class.", nameof(callback));
        }
        unsafe
        {
            fixed (uint* handlePtr = &handle)
            {
                return UEnhancedInputComponentExporter.CallBindAction(NativeObject, action.NativeObject, triggerEvent, unrealObject.NativeObject, callback.Method.Name, (IntPtr) handlePtr);
            }
        }
    }

    public bool BindAction(UInputAction action, ETriggerEvent triggerEvent,
        Action<FInputActionValue, float, float, UInputAction> callback) =>
        BindAction(action, triggerEvent, callback, out var dummy);

    public bool RemoveBinding(uint handle)
    {
        return UEnhancedInputComponentExporter.CallRemoveBindingByHandle(NativeObject, handle);
    }
}