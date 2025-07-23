using System.Reflection;
using UnrealSharp.Core;
using UnrealSharp.Core.Attributes;
using UnrealSharp.Engine;
using UnrealSharp.Interop;
using UnrealSharp.UMG;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.CoreUObject;

public partial class UObject
{
    /// <summary>
    /// The name of the object in Unreal Engine.
    /// </summary>
    public FName ObjectName
    {
        get
        {
            if (IsDestroyed)
            {
                return FName.None;
            }

            UObjectExporter.CallNativeGetName(NativeObject, out FName objectName);
            return objectName;
        }
    }

    /// <summary>
    /// Whether the object is valid. UObjects can be valid but pending kill.
    /// </summary>
    public bool IsValid => !IsDestroyed;

    /// <summary>
    /// Whether the object has been destroyed.
    /// </summary>
    public bool IsDestroyed => NativeObject == IntPtr.Zero || !UObjectExporter.CallNativeIsValid(NativeObject).ToManagedBool();

    /// <summary>
    /// The unique ID of the object... These are reused so it is only unique while the object is alive.
    /// </summary>
    public int UniqueID => UObjectExporter.CallGetUniqueID(NativeObject);

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

            IntPtr worldPtr = UObjectExporter.CallGetWorld_Internal(NativeObject);
            UWorld? foundWorld = GCHandleUtilities.GetObjectFromHandlePtr<UWorld>(worldPtr);

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

    public static implicit operator bool(UObject Object)
    {
        return Object != null && UObjectExporter.CallNativeIsValid(Object.NativeObject).ToManagedBool();
    }

    /// <summary>
    /// Prints a message to the screen and/or console.
    /// </summary>
    /// <param name="message"> The message to print. </param>
    /// <param name="duration"> The duration to display the message. </param>
    /// <param name="color"> The color of the message. </param>
    /// <param name="printToScreen"> Whether to print the message to the screen. </param>
    /// <param name="printToConsole"> Whether to print the message to the console. </param>
    /// <param name="key"> Whether to print the message to the console. </param>
    /// <param name="key"> If a non-empty key is provided, the message will replace any existing on-screen messages with the same key. </param>
    public static void PrintString(string message = "Hello", float duration = 2.0f, FLinearColor color = default, bool printToScreen = true, bool printToConsole = true, string key = "")
    {
        if (color.IsZero())
        {
            // Use the default color if none is provided
            color = new FLinearColor
            {
                R = 0.0f,
                G = 0.66f,
                B = 1.0f,
                A = 1.0f
            };
        }

        UCSSystemExtensions.PrintStringInternal(message, printToScreen, printToConsole, color, duration, key);
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
    public static T NewObject<T>(UObject? outer = null, TSubclassOf<T> classType = default, UObject? template = null) where T : UnrealSharpObject
    {
        if (classType.NativeClass == IntPtr.Zero)
        {
            classType = new TSubclassOf<T>();
        }

        IntPtr nativeTemplate = template?.NativeObject ?? IntPtr.Zero;

        if (outer == null || outer.NativeObject == IntPtr.Zero)
        {
            outer = GetTransientPackage();
        }

        IntPtr handle = UObjectExporter.CallCreateNewObject(outer.NativeObject, classType.NativeClass, nativeTemplate);
        return GCHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }

    /// <summary>
    /// Gets the transient package.
    /// </summary>
    /// <returns> The transient package. </returns>
    public static UPackage GetTransientPackage()
    {
        IntPtr handle = UObjectExporter.CallGetTransientPackage();
        return GCHandleUtilities.GetObjectFromHandlePtr<UPackage>(handle)!;
    }

    /// <summary>
    /// Get the default object of the specified type.
    /// </summary>
    /// <typeparam name="T"> The type of the object to get. </typeparam>
    /// <returns> The default object of the specified type. </returns>
    public static T GetDefault<T>() where T : UObject
    {
        IntPtr nativeClass = typeof(T).TryGetNativeClassDefaults();
        return GCHandleUtilities.GetObjectFromHandlePtr<T>(nativeClass)!;
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
        return GCHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
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
    public static T SpawnActor<T>(TSubclassOf<T> actorType = default,
        FTransform spawnTransform = default,
        ESpawnActorCollisionHandlingMethod spawnMethod = ESpawnActorCollisionHandlingMethod.Undefined,
        APawn? instigator = null,
        AActor? owner = null) where T : AActor
    {
        FCSSpawnActorParameters actorSpawnParameters = new FCSSpawnActorParameters
        {
            Instigator = instigator,
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
    public static T SpawnActor<T>(FTransform spawnTransform, TSubclassOf<T> actorType, FCSSpawnActorParameters spawnParameters) where T : AActor
    {
        return (T) UCSWorldExtensions.SpawnActor(new TSubclassOf<AActor>(actorType), spawnTransform, spawnParameters);
    }

    /// <summary>
    /// Spawns an actor of the specified type, with a callback to initialize the actor.
    /// </summary>
    /// <param name="spawnTransform"> The transform to spawn the actor at. </param>
    /// <param name="actorType"> The type of the actor to spawn. </param>
    /// <param name="spawnParameters"> The parameters to use when spawning the actor. </param>
    /// <param name="initializeActor"> Callback to initialize actor properties. C# spawned components are not yet valid here.</param>
    /// <param name="initializeComponents"> Callback to initialize components properties. Both actor and components are valid here.</param>
    /// <typeparam name="T"> The type of the actor to spawn. </typeparam>
    /// <returns> The spawned actor. </returns>
    public static T SpawnActorDeferred<T>(FTransform spawnTransform, TSubclassOf<T> actorType, FCSSpawnActorParameters spawnParameters, Action<T>? initializeActor = null, Action<T>? initializeComponents = null) where T : AActor
    {
        T spawnedActor = (T) UCSWorldExtensions.SpawnActorDeferred(new TSubclassOf<AActor>(actorType), spawnTransform, spawnParameters);

        if (initializeActor != null)
        {
            initializeActor(spawnedActor);
        }

        UCSWorldExtensions.ExecuteConstruction(spawnedActor, spawnTransform);

        if (initializeComponents != null)
        {
            initializeComponents(spawnedActor);
        }

        UCSWorldExtensions.PostActorConstruction(spawnedActor);
        return spawnedActor;
    }

    /// <summary>
    /// Gets the world subsystem of the specified type.
    /// </summary>
    /// <param name="subsystemClass"> The type of the subsystem to get. </param>
    /// <typeparam name="T"> The type of the subsystem to get. </typeparam>
    /// <returns> The world subsystem of the specified type. </returns>
    public T GetWorldSubsystem<T>(TSubclassOf<T> subsystemClass) where T : UWorldSubsystem
    {
        IntPtr handle = UWorldExporter.CallGetWorldSubsystem(subsystemClass.NativeClass, NativeObject);
        return GCHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }

    /// <summary>
    /// Gets the world subsystem of the specified type.
    /// </summary>
    /// <param name="subsystemClass"> The type of the subsystem to get. </param>
    /// <returns> The world subsystem of the specified type. </returns>
    public UWorldSubsystem GetWorldSubsystem(Type subsystemClass)
    {
        return GetWorldSubsystem(new TSubclassOf<UWorldSubsystem>(subsystemClass));
    }

    /// <summary>
    /// Gets the world subsystem of the specified type.
    /// </summary>
    /// <typeparam name="T"> The type of the subsystem to get. </typeparam>
    /// <returns> The world subsystem of the specified type. </returns>
    public T GetWorldSubsystem<T>() where T : UWorldSubsystem
    {
        return GetWorldSubsystem(new TSubclassOf<T>(typeof(T)));
    }

    /// <summary>
    /// Gets the game instance subsystem of the specified type.
    /// </summary>
    /// <param name="subsystemClass"> The type of the subsystem to get. </param>
    /// <typeparam name="T"> The type of the subsystem to get. </typeparam>
    /// <returns> The game instance subsystem of the specified type. </returns>
    public T GetGameInstanceSubsystem<T>(TSubclassOf<T> subsystemClass) where T : UGameInstanceSubsystem
    {
        IntPtr handle = UGameInstanceExporter.CallGetGameInstanceSubsystem(subsystemClass.NativeClass, NativeObject);
        return GCHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }

    /// <summary>
    /// Gets the game instance subsystem of the specified type.
    /// </summary>
    /// <param name="subsystemClass"> The type of the subsystem to get. </param>
    /// <returns> The game instance subsystem of the specified type. </returns>
    public UGameInstanceSubsystem GetGameInstanceSubsystem(Type subsystemClass)
    {
        return GetGameInstanceSubsystem(new TSubclassOf<UGameInstanceSubsystem>(subsystemClass));
    }

    /// <summary>
    /// Gets the game instance subsystem of the specified type.
    /// </summary>
    /// <typeparam name="T"> The type of the subsystem to get. </typeparam>
    /// <returns> The game instance subsystem of the specified type. </returns>
    public T GetGameInstanceSubsystem<T>() where T : UGameInstanceSubsystem
    {
        return GetGameInstanceSubsystem(new TSubclassOf<T>(typeof(T)));
    }

#if WITH_EDITOR
    /// <summary>
    /// Gets the editor subsystem of the specified type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetEditorSubsystem<T>(TSubclassOf<T> subsystemClass) where T : EditorSubsystem.UEditorSubsystem
    {
        IntPtr handle = GEditorExporter.CallGetEditorSubsystem(subsystemClass.NativeClass);
        return GCHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }

    public static EditorSubsystem.UEditorSubsystem GetEditorSubsystem(Type subsystemClass)
    {
        return GetEditorSubsystem(new TSubclassOf<EditorSubsystem.UEditorSubsystem>(subsystemClass));
    }

    /// <summary>
    /// Gets the editor subsystem of the specified type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetEditorSubsystem<T>() where T : EditorSubsystem.UEditorSubsystem
    {
        return GetEditorSubsystem<T>(new TSubclassOf<T>(typeof(T)));
    }
#endif

    /// <summary>
    /// Gets the engine subsystem of the specified type.
    /// </summary>
    /// <param name="subsystemClass"> The type of the subsystem to get. </param>
    /// <typeparam name="T"> The type of the subsystem to get. </typeparam>
    /// <returns> The engine subsystem of the specified type. </returns>
    public static T GetEngineSubsystem<T>(TSubclassOf<T> subsystemClass) where T : UEngineSubsystem
    {
        IntPtr handle = GEngineExporter.CallGetEngineSubsystem(subsystemClass.NativeClass);
        return GCHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }

    /// <summary>
    /// Gets the engine subsystem of the specified type.
    /// </summary>
    /// <param name="subsystemClass"> The type of the subsystem to get. </param>
    /// <returns> The engine subsystem of the specified type. </returns>
    public static UEngineSubsystem GetEngineSubsystem(Type subsystemClass)
    {
        return GetEngineSubsystem(new TSubclassOf<UEngineSubsystem>(subsystemClass));
    }

    /// <summary>
    /// Gets the engine subsystem of the specified type.
    /// </summary>
    /// <typeparam name="T"> The type of the subsystem to get. </typeparam>
    /// <returns> The engine subsystem of the specified type. </returns>
    public static T GetEngineSubsystem<T>() where T : UEngineSubsystem
    {
        var subsystemClass = new TSubclassOf<T>(typeof(T));
        IntPtr handle = GEngineExporter.CallGetEngineSubsystem(subsystemClass.NativeClass);
        return GCHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }

    /// <summary>
    /// Gets the local player subsystem of the specified type.
    /// </summary>
    /// <param name="playerController"> The player controller to get the subsystem from. </param>
    /// <param name="subsystemClass"> The type of the subsystem to get. </param>
    /// <typeparam name="T"> The type of the subsystem to get. </typeparam>
    /// <returns> The local player subsystem of the specified type. </returns>
    public static T GetLocalPlayerSubsystem<T>(APlayerController playerController, TSubclassOf<T> subsystemClass) where T : ULocalPlayerSubsystem
    {
        IntPtr handle = ULocalPlayerExporter.CallGetLocalPlayerSubsystem(subsystemClass.NativeClass, playerController.NativeObject);
        return GCHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }

    /// <summary>
    /// Gets the local player subsystem of the specified type.
    /// </summary>
    /// <param name="playerController"> The player controller to get the subsystem from. </param>
    /// <param name="subsystemClass"> The type of the subsystem to get. </param>
    /// <returns> The local player subsystem of the specified type. </returns>
    public static ULocalPlayerSubsystem GetLocalPlayerSubsystem(APlayerController playerController, Type subsystemClass)
    {
        return GetLocalPlayerSubsystem(playerController, new TSubclassOf<ULocalPlayerSubsystem>(subsystemClass));
    }

    /// <summary>
    /// Gets the local player subsystem of the specified type.
    /// </summary>
    /// <param name="playerController"> The player controller to get the subsystem from. </param>
    /// <typeparam name="T"> The type of the subsystem to get. </typeparam>
    /// <returns> The local player subsystem of the specified type. </returns>
    public static T GetLocalPlayerSubsystem<T>(APlayerController playerController) where T : ULocalPlayerSubsystem
    {
        return GetLocalPlayerSubsystem(playerController, new TSubclassOf<T>(typeof(T)));
    }

    /// <summary>
    /// Creates a widget of the specified type.
    /// </summary>
    /// <param name="widgetClass"> The class of the widget to create. </param>
    /// <param name="owningController"> The owning player controller. </param>
    /// <typeparam name="T"> The type of the widget to create. </typeparam>
    /// <returns></returns>
    public static T CreateWidget<T>(TSubclassOf<T> widgetClass, APlayerController? owningController = null) where T : UUserWidget
    {
        return UCSUserWidgetExtensions.CreateWidget(widgetClass, owningController);
    }

    /// <summary>
    /// Marks the object as garbage.
    /// </summary>
    public void MarkAsGarbage() => UCSObjectExtensions.MarkAsGarbage(this);

    /// <summary>
    /// Gets the class of the object.
    /// </summary>
    public UClass Class => UCSObjectExtensions.GetClass(this);

    /// <summary>
    /// Determines whether the object is a template / class default object.
    /// </summary>
    public bool IsTemplate => UCSObjectExtensions.IsTemplate(this);

    /// <summary>
    /// Gets the current world settings for this object.
    /// </summary>
    public AWorldSettings WorldSettings => UCSObjectExtensions.GetWorldSettings(this);

    /// <summary>
    /// Gets the world settings as the specified type.
    /// </summary>
    /// <typeparam name="T"> The type of the world settings to get. </typeparam>
    /// <returns> The world settings as the specified type. </returns>
    public T GetWorldSettingsAs<T>() where T : AWorldSettings
    {
        return (T) WorldSettings;
    }
}

internal static class ReflectionHelper
{
    // Get the name without the U/A/F/E prefix.
    internal static string GetEngineName(this Type type)
    {
        Attribute? generatedTypeAttribute = type.GetCustomAttribute<GeneratedTypeAttribute>();

        if (generatedTypeAttribute is null)
        {
            return type.Name;
        }

        FieldInfo? field = generatedTypeAttribute.GetType().GetField("EngineName");

        if (field == null)
        {
            throw new InvalidOperationException($"The EngineName field was not found in the {nameof(GeneratedTypeAttribute)}.");
        }

        return (string) field.GetValue(generatedTypeAttribute)!;
    }

    internal static IntPtr TryGetNativeClass(this Type type)
    {
        return UCoreUObjectExporter.CallGetNativeClassFromName(type.GetAssemblyName(), type.Namespace, type.GetEngineName());
    }
    
    internal static IntPtr TryGetNativeClassDefaults(this Type type)
    {
        return UClassExporter.CallGetDefaultFromName(type.GetAssemblyName(), type.Namespace, type.GetEngineName());
    }
}
