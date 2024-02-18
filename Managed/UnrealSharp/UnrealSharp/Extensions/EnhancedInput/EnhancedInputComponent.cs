using System.Runtime.InteropServices;
using UnrealSharp.Engine;
using UnrealSharp.Interop;

namespace UnrealSharp.EnhancedInput;

public partial class EnhancedInputComponent
{
    public void BindAction(InputAction action, ETriggerEvent triggerEvent, Action<InputActionValue> callback)
    {
        if (callback.Target is CoreUObject.Object unrealObject)
        {
            UEnhancedInputComponentExporter.CallBindAction(NativeObject, action.NativeObject, triggerEvent, unrealObject.NativeObject, callback.Method.Name);
        }
    }
}