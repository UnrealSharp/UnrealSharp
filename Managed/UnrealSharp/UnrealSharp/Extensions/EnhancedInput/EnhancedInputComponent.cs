using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp.EnhancedInput;

public partial class UEnhancedInputComponent
{
    public void BindAction(UInputAction action, ETriggerEvent triggerEvent, Action<FInputActionValue, float, float, UInputAction> callback)
    {
        if (callback.Target is not UObject unrealObject)
        {
            throw new ArgumentException("The callback must be a method within a UObject class.", nameof(callback));
        }
        
        UEnhancedInputComponentExporter.CallBindAction(NativeObject, action.NativeObject, triggerEvent, unrealObject.NativeObject, callback.Method.Name);
    }
}