using UnrealSharp.EnhancedInput;
using UnrealSharp.Interop;

namespace UnrealSharp.Engine;

public partial class Actor
{
    public InputComponent? InputComponent
    {
        get
        {
            IntPtr inputComponent = AActorExporter.CallGetInputComponent(NativeObject);
            return GcHandleUtilities.GetObjectFromHandlePtr<InputComponent>(inputComponent);
        }
    }
    
    public void BindAction(string actionName, EInputEvent inputEvent, Action action, bool consumeInput = false, bool executeWhenPaused = false)
    {
        InputComponent? inputComponent = InputComponent;
        
        if (inputComponent != null)
        {
            inputComponent.BindAction(actionName, inputEvent, action, consumeInput, executeWhenPaused);
        }
    }

    public void BindAction(InputAction action, ETriggerEvent triggerEvent, Action<InputActionValue> callback)
    {
        if (InputComponent is EnhancedInputComponent enhancedInputComponent)
        {
            enhancedInputComponent.BindAction(action, triggerEvent, callback);
        }
    }

    public void BindAxis(string axisName, Action<float> action, bool consumeInput = false, bool executeWhenPaused = false)
    {
        InputComponent? inputComponent = InputComponent;
        
        if (inputComponent != null)
        {
            inputComponent.BindAxis(axisName, action, consumeInput, executeWhenPaused);
        }
    }

    public T AddComponentByClass<T>(bool bManualAttachment, Transform relativeTransform) where T : ActorComponent
        => (AddComponentByClass(new SubclassOf<ActorComponent>(typeof(T)), bManualAttachment, relativeTransform, bDeferredFinish: false) as T)!;

    public T AddComponentByClass<T>(bool bManualAttachment, Transform relativeTransform, Action<T> initializerFunc) where T : ActorComponent
    {
        T component = (AddComponentByClass(new SubclassOf<ActorComponent>(typeof(T)), bManualAttachment, relativeTransform, bDeferredFinish: true) as T)!;
        initializerFunc(component);
        FinishAddComponent(component, bManualAttachment, relativeTransform);
        return component;
    }

    public ActorComponent AddComponentByClass(SubclassOf<ActorComponent> @class, bool bManualAttachment, Transform relativeTransform)
        => AddComponentByClass(@class, bManualAttachment, relativeTransform, bDeferredFinish: false);
}