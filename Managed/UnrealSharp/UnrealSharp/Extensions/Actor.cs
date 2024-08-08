using UnrealSharp.CoreUObject;
using UnrealSharp.EnhancedInput;

namespace UnrealSharp.Engine;

public partial class AActor
{
    /// <summary>
    /// Bind an action to a callback.
    /// </summary>
    /// <param name="actionName"> The name of the action to bind </param>
    /// <param name="inputEvent"> The input event to bind the action to </param>
    /// <param name="action"> The action to execute when the action is triggered </param>
    /// <param name="consumeInput"> Whether to consume the input </param>
    /// <param name="executeWhenPaused"> Whether to execute the action when paused </param>
    public void BindAction(string actionName, EInputEvent inputEvent, Action action, bool consumeInput = false, bool executeWhenPaused = false)
    {
        UInputComponent? inputComponent = InputComponent;
        if (inputComponent != null)
        {
            inputComponent.BindAction(actionName, inputEvent, action, consumeInput, executeWhenPaused);
        }
    }

    /// <summary>
    /// Binds an action to a callback.
    /// </summary>
    /// <param name="action"> The action to bind </param>
    /// <param name="triggerEvent"> The trigger event to bind the action to </param>
    /// <param name="callback"> The callback to execute when the action is triggered </param>
    public void BindAction(UInputAction action, ETriggerEvent triggerEvent, Action<FInputActionValue> callback)
    {
        if (InputComponent is UEnhancedInputComponent enhancedInputComponent)
        {
            enhancedInputComponent.BindAction(action, triggerEvent, callback);
        }
    }

    /// <summary>
    /// Binds an axis to an action.
    /// </summary>
    /// <param name="axisName"> The name of the axis to bind </param>
    /// <param name="action"> The action to bind the axis to </param>
    /// <param name="consumeInput"> Whether to consume the input </param>
    /// <param name="executeWhenPaused"> Whether to execute the action when paused </param>
    public void BindAxis(string axisName, Action<float> action, bool consumeInput = false, bool executeWhenPaused = false)
    {
        UInputComponent? inputComponent = InputComponent;
        
        if (inputComponent != null)
        {
            inputComponent.BindAxis(axisName, action, consumeInput, executeWhenPaused);
        }
    }

    /// <summary>
    /// Adds a component to the actor by class.
    /// </summary>
    /// <param name="bManualAttachment">Whether to manually attach the component</param>
    /// <param name="relativeTransform">Set the relative transform of the component</param>
    /// <typeparam name="T">Class of the component to add</typeparam>
    /// <returns>The component</returns>
    public T AddComponentByClass<T>(bool bManualAttachment, FTransform relativeTransform) where T : UActorComponent
        => (AddComponentByClass(new TSubclassOf<UActorComponent>(typeof(T)), bManualAttachment, relativeTransform, deferredFinish: false) as T)!;

    /// <summary>
    /// Adds a component to the actor by class.
    /// </summary>
    /// <param name="bManualAttachment">Whether to manually attach the component</param>
    /// <param name="relativeTransform">Set the relative transform of the component</param>
    /// <param name="initializerFunc"> The function to initialize the component </param>
    /// <typeparam name="T">Class of the component to add</typeparam>
    /// <returns>The component</returns>
    public T AddComponentByClass<T>(bool bManualAttachment, FTransform relativeTransform, Action<T> initializerFunc) where T : UActorComponent
    {
        T component = (AddComponentByClass(new TSubclassOf<UActorComponent>(typeof(T)), bManualAttachment, relativeTransform, deferredFinish: true) as T)!;
        initializerFunc(component);
        FinishAddComponent(component, bManualAttachment, relativeTransform);
        return component;
    }

    /// <summary>
    /// Adds a component to the actor by class.
    /// </summary>
    /// <param name="class">Class of the component to get</param>
    /// <param name="bManualAttachment">Whether to manually attach the component</param>
    /// <param name="relativeTransform">Set the relative transform of the component</param>
    /// <returns>The component if added, otherwise null</returns>
    public UActorComponent AddComponentByClass(TSubclassOf<UActorComponent> @class, bool bManualAttachment, FTransform relativeTransform) 
        => AddComponentByClass(@class, bManualAttachment, relativeTransform, deferredFinish: false);
 
    /// <summary>
    /// Tries to get a component by class, will return null if the component is not found.
    /// </summary>
    /// <typeparam name="T">The type of the component to get</typeparam>
    /// <param name="class">The class of the component to get. Can be left null.</param>
    /// <returns>The component if found, otherwise null</returns>
    public T? GetComponentByClass<T>(TSubclassOf<UActorComponent>? @class = null) where T : UActorComponent
    {
        if (@class == null)
        {
            @class = new TSubclassOf<UActorComponent>(typeof(T));
        }
        
        return GetComponentByClass(@class.Value) as T;
    }
}