using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp.EnhancedInput;

public partial class UEnhancedInputComponent
{
    public void BindAction(UInputAction action, ETriggerEvent triggerEvent, Action<FInputActionValue> callback)
    {
        if (callback.Target is UObject unrealObject)
        {
            UEnhancedInputComponentExporter.CallBindAction(NativeObject, action.NativeObject, triggerEvent, unrealObject.NativeObject, callback.Method.Name);
        }
    }
}