using UnrealSharp.Interop;
using Object = UnrealSharp.CoreUObject.Object;

namespace UnrealSharp.Engine;

public partial class InputComponent
{
    public void BindAction(string actionName, EInputEvent inputEvent, Action action, bool consumeInput = false, bool executeWhenPaused = false)
    {
        if (action.Target is Object unrealObject)
        {
            UInputComponentExporter.CallBindAction(NativeObject, 
                actionName, 
                inputEvent, 
                unrealObject.NativeObject, 
                action.Method.Name,
                consumeInput.ToNativeBool(),
                executeWhenPaused.ToNativeBool());
        }
    }

    public void BindAxis(string axisName, Action<float> action, bool consumeInput = false, bool executeWhenPaused = false)
    {
        if (action.Target is Object unrealObject)
        {
            UInputComponentExporter.CallBindAxis(NativeObject,
                axisName, 
                unrealObject.NativeObject, 
                action.Method.Name,
                consumeInput.ToNativeBool(),
                executeWhenPaused.ToNativeBool());
        }
    }
}