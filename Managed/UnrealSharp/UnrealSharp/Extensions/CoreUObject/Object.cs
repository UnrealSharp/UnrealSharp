using UnrealSharp.Engine;
using UnrealSharp.Interop;
using UnrealSharp.UMG;

namespace UnrealSharp.CoreUObject;

public partial class UObject
{
    /// <summary>
    /// The name of the object in Unreal Engine.
    /// </summary>
    public FName ObjectName => IsDestroyed ? FName.None : UObjectExporter.CallNativeGetName(NativeObject);
    
    /// <summary>
    /// Whether the object is valid. UObjects can be valid but pending kill.
    /// </summary>
    public bool IsValid => !IsDestroyed;
    
    /// <summary>
    /// Whether the object has been destroyed.
    /// </summary>
    public bool IsDestroyed => NativeObject == IntPtr.Zero || !UObjectExporter.CallNativeIsValid(NativeObject);

    /// <summary>
    /// The world that the object belongs to.
    /// </summary>
    public UWorld World
    {
        get
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Object is not valid.");
            }
            
            IntPtr worldPtr = UObjectExporter.CallGetWorld(NativeObject);
            UWorld? foundWorld = GcHandleUtilities.GetObjectFromHandlePtr<UWorld>(worldPtr);
            
            if (foundWorld == null)
            {
                throw new InvalidOperationException("World is not valid.");
            }
            
            return foundWorld;
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return ObjectName.ToString();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is UnrealSharpObject unrealSharpObject && NativeObject == unrealSharpObject.NativeObject;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return NativeObject.GetHashCode();
    }
    
    /// <summary>
    /// Prints a message to the screen and/or console.
    /// </summary>
    /// <param name="message"> The message to print. </param>
    /// <param name="duration"> The duration to display the message. </param>
    /// <param name="color"> The color of the message. </param>
    /// <param name="printToScreen"> Whether to print the message to the screen. </param>
    /// <param name="printToConsole"> Whether to print the message to the console. </param>
    public void PrintString(string message = "Hello", float duration = 2.0f, FLinearColor color = default, bool printToScreen = true, bool printToConsole = true)
    {
        unsafe
        {
            fixed (char* messagePtr = message)
            {
                // Use the default color if none is provided
                if (color.IsZero())
                {
                    color = new FLinearColor
                    {
                        R = 0.0f,
                        G = 0.66f,
                        B = 1.0f,
                        A = 1.0f
                    };
                }
                
                UKismetSystemLibraryExporter.CallPrintString(
                    NativeObject, 
                    (IntPtr) messagePtr, 
                    duration, 
                    color, 
                    printToScreen.ToNativeBool(), 
                    printToConsole.ToNativeBool());
            }
        }
    }
    
    /// <summary>
    /// Prints a message to the console.
    /// </summary>
    /// <param name="message"> The message to print. </param>
    public void PrintToConsole(string message = "Hello")
    {
        PrintString(message, printToScreen: false);
    }
    
    /// <summary>
    /// Creates a new object of the specified type.
    /// </summary>
    /// <param name="outer"> The outer object. </param>
    /// <param name="classType"> The type of the object to create. </param>
    /// <param name="template"> The template object to use. All the property values from this template will be copied. </param>
    /// <typeparam name="T"> The type of the object to create. </typeparam>
    /// <returns> The newly created object. </returns>
    /// <exception cref="ArgumentException"> Thrown if the outer object is not valid. </exception>
    public static T NewObject<T>(UObject outer, TSubclassOf<T> classType = default, UObject? template = null) where T : UnrealSharpObject
    {
        if (classType.NativeClass == IntPtr.Zero)
        {
            classType = new TSubclassOf<T>();
        }
        
        IntPtr nativeOuter = outer?.NativeObject ?? IntPtr.Zero;
        IntPtr nativeTemplate = template?.NativeObject ?? IntPtr.Zero;

        if (nativeOuter == IntPtr.Zero)
        {
            throw new ArgumentException("Outer must be a valid object", nameof(outer));
        }
        
        IntPtr handle = UObjectExporter.CallCreateNewObject(nativeOuter, classType.NativeClass, nativeTemplate);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }

    /// <summary>
    /// Gets the transient package.
    /// </summary>
    /// <returns> The transient package. </returns>
    public static UPackage? GetTransientPackage()
    {
        IntPtr handle = UObjectExporter.CallGetTransientPackage();
        return GcHandleUtilities.GetObjectFromHandlePtr<UPackage>(handle);
    }
    
    /// <summary>
    /// Get the default object of the specified type.
    /// </summary>
    /// <typeparam name="T"> The type of the object to get. </typeparam>
    /// <returns> The default object of the specified type. </returns>
    public static T GetDefault<T>() where T : UObject
    {
        IntPtr handle = UClassExporter.CallGetDefaultFromString(typeof(T).Name);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }
    
    /// <summary>
    /// Get the default object of the specified type.
    /// </summary>
    /// <param name="obj"> The object to get the default object from. </param>
    /// <typeparam name="T"> The type of the object to get. </typeparam>
    /// <returns> The default object of the specified type. </returns>
    public static T GetDefault<T>(UObject obj) where T : UnrealSharpObject
    {
        IntPtr handle = UClassExporter.CallGetDefaultFromInstance(obj.NativeObject);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }
    
    /// <summary>
    /// Spawns an actor of the specified type.
    /// </summary>
    /// <param name="actorType"> The type of the actor to spawn. </param>
    /// <param name="spawnTransform"> The transform to spawn the actor at. </param>
    /// <param name="spawnMethod"> The method to use when spawning the actor. </param>
    /// <param name="instigator"> The instigator of the actor. </param>
    /// <param name="owner"> The owner of the actor. </param>
    /// <typeparam name="T"> The type of the actor to spawn. </typeparam>
    /// <returns> The spawned actor. </returns>
    public T SpawnActor<T>(TSubclassOf<T> actorType = default, 
        FTransform spawnTransform = default,
        ESpawnActorCollisionHandlingMethod spawnMethod = ESpawnActorCollisionHandlingMethod.Undefined, 
        APawn? instigator = null, 
        AActor? owner = null) where T : AActor
    {
        FActorSpawnParameters actorSpawnParameters = new FActorSpawnParameters
        {
            Instigator = instigator,
            DeferConstruction = false,
            Owner = owner,
            SpawnMethod = spawnMethod,
        };
        
        return SpawnActor(spawnTransform, actorType, actorSpawnParameters);
    }
    
    /// <summary>
    /// Spawns an actor of the specified type.
    /// </summary>
    /// <param name="spawnTransform"> The transform to spawn the actor at. </param>
    /// <param name="actorType"> The type of the actor to spawn. </param>
    /// <param name="spawnParameters"> The parameters to use when spawning the actor. </param>
    /// <typeparam name="T"> The type of the actor to spawn. </typeparam>
    /// <returns> The spawned actor. </returns>
    public T SpawnActor<T>(FTransform spawnTransform, TSubclassOf<T> actorType, FActorSpawnParameters spawnParameters) where T : AActor
    {
        unsafe
        {
            IntPtr handle = UWorldExporter.CallSpawnActor(NativeObject, &spawnTransform, actorType.NativeClass, ref spawnParameters);
            return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
        }
    }
    
    /// <summary>
    /// Gets the world subsystem of the specified type.
    /// </summary>
    /// <typeparam name="T"> The type of the subsystem to get. </typeparam>
    /// <returns> The world subsystem of the specified type. </returns>
    public T GetWorldSubsystem<T>() where T : UWorldSubsystem
    {
        var subsystemClass = new TSubclassOf<T>(typeof(T));
        IntPtr handle = UWorldExporter.CallGetWorldSubsystem(subsystemClass.NativeClass, NativeObject);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }
    
    /// <summary>
    /// Gets the game instance subsystem of the specified type.
    /// </summary>
    /// <typeparam name="T"> The type of the subsystem to get. </typeparam>
    /// <returns> The game instance subsystem of the specified type. </returns>
    public T GetGameInstanceSubsystem<T>() where T : UGameInstanceSubsystem
    {
        var subsystemClass = new TSubclassOf<T>(typeof(T));
        IntPtr handle = UGameInstanceExporter.CallGetGameInstanceSubsystem(subsystemClass.NativeClass, NativeObject);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }

#if WITH_EDITOR
    /// <summary>
    /// Gets the editor subsystem of the specified type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetEditorSubsystem<T>() where T : EditorSubsystem.EditorSubsystem
    {
        var subsystemClass = new SubclassOf<T>(typeof(T));
        IntPtr handle = GEditorExporter.CallGetEditorSubsystem(subsystemClass.NativeClass);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }
#endif
    
    /// <summary>
    /// Gets the engine subsystem of the specified type.
    /// </summary>
    /// <typeparam name="T"> The type of the subsystem to get. </typeparam>
    /// <returns> The engine subsystem of the specified type. </returns>
    public T GetEngineSubsystem<T>() where T : UEngineSubsystem
    {
        var subsystemClass = new TSubclassOf<T>(typeof(T));
        IntPtr handle = GEngineExporter.CallGetEngineSubsystem(subsystemClass.NativeClass);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }
    
    /// <summary>
    /// Gets the local player subsystem of the specified type.
    /// </summary>
    /// <param name="playerController"> The player controller to get the subsystem from. </param>
    /// <typeparam name="T"> The type of the subsystem to get. </typeparam>
    /// <returns> The local player subsystem of the specified type. </returns>
    public T GetLocalPlayerSubsystem<T>(APlayerController playerController) where T : ULocalPlayerSubsystem
    {
        var subsystemClass = new TSubclassOf<T>(typeof(T));
        IntPtr handle = ULocalPlayerExporter.CallGetLocalPlayerSubsystem(subsystemClass.NativeClass, playerController.NativeObject);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }
    
    /// <summary>
    /// Creates a widget of the specified type.
    /// </summary>
    /// <param name="widgetClass"> The class of the widget to create. </param>
    /// <param name="owningController"> The owning player controller. </param>
    /// <typeparam name="T"> The type of the widget to create. </typeparam>
    /// <returns></returns>
    public T CreateWidget<T>(TSubclassOf<T> widgetClass, APlayerController? owningController = null) where T : UUserWidget
    {
        unsafe
        {
            IntPtr owningPlayerPtr = owningController?.NativeObject ?? IntPtr.Zero;
            IntPtr handle = UWidgetBlueprintLibraryExporter.CreateWidget(NativeObject, widgetClass.NativeClass, owningPlayerPtr);
            return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
        }
    }
}