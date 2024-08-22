using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp.Engine;

public partial class UInputComponent
{
    /// <summary>
    /// Bind an action to an input event.
    /// </summary>
    /// <param name="actionName"> The name of the action. </param>
    /// <param name="inputEvent"> The input event to bind the action to. </param>
    /// <param name="action"> The action to bind. </param>
    /// <param name="consumeInput"> Whether the input should be consumed. </param>
    /// <param name="executeWhenPaused"> Whether the action should execute when the game is paused. </param>
    public void BindAction(string actionName, EInputEvent inputEvent, Action action, bool consumeInput = false, bool executeWhenPaused = false)
    {
        if (action.Target is UObject unrealObject)
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
    
    /// <summary>
    /// Bind an action to an input event.
    /// </summary>
    /// <param name="actionName"> The name of the action. </param>
    /// <param name="inputEvent"> The input event to bind the action to. </param>
    /// <param name="action"> The action to bind with key signature. </param>
    /// <param name="consumeInput"> Whether the input should be consumed. </param>
    /// <param name="executeWhenPaused"> Whether the action should execute when the game is paused. </param>
    public void BindAction(string actionName, EInputEvent inputEvent, Action<InputCore.FKey> action, bool consumeInput = false, bool executeWhenPaused = false)
    {
        if (action.Target is UObject unrealObject)
        {
            UInputComponentExporter.CallBindActionKeySignature(NativeObject, 
                actionName, 
                inputEvent, 
                unrealObject.NativeObject, 
                action.Method.Name,
                consumeInput.ToNativeBool(),
                executeWhenPaused.ToNativeBool());
        }
    }

    /// <summary>
    /// Bind an axis to an input event.
    /// </summary>
    /// <param name="axisName"> The name of the axis. </param>
    /// <param name="action"> The action to bind. </param>
    /// <param name="consumeInput"> Whether the input should be consumed. </param>
    /// <param name="executeWhenPaused"> Whether the action should execute when the game is paused. </param>
    public void BindAxis(string axisName, Action<float> action, bool consumeInput = false, bool executeWhenPaused = false)
    {
        if (action.Target is UObject unrealObject)
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