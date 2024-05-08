using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnrealSharp.CoreUObject;
using UnrealSharp.CSharpForUE;
using UnrealSharp.Engine;
using UnrealSharp.Interop;
using UnrealSharp.UMG;

namespace UnrealSharp;

[Serializable]
public class UnrealObjectDestroyedException : InvalidOperationException
{
    public UnrealObjectDestroyedException()
    {

    }

    public UnrealObjectDestroyedException(string message)
        : base(message)
    {

    }

    public UnrealObjectDestroyedException(string message, Exception innerException)
        : base(message, innerException)
    {

    }
}

/// <summary>
/// Represents a UObject in Unreal Engine. Don't inherit from this class directly, use a CoreUObject.Object instead.
/// </summary>
public class UnrealSharpObject : IDisposable
{
    internal static IntPtr Create(Type typeToCreate, IntPtr nativeObjectPtr)
    {
        unsafe
        {
            UnrealSharpObject createdObject = (UnrealSharpObject) RuntimeHelpers.GetUninitializedObject(typeToCreate);
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var foundConstructor = (delegate*<object, void>) typeToCreate.GetConstructor(bindingFlags, Type.EmptyTypes)!.MethodHandle.GetFunctionPointer();
            createdObject.NativeObject = nativeObjectPtr;
            foundConstructor(createdObject);
            return GCHandle.ToIntPtr(GcHandleUtilities.AllocateStrongPointer(createdObject));
        }
    }
    
    /// <summary>
    /// The pointer to the UObject that this C# object represents.
    /// </summary>
    public IntPtr NativeObject { get; private set; }
    
    
    /// <summary>
    /// The name of the object in Unreal Engine.
    /// </summary>
    public Name ObjectName => IsDestroyed ? Name.None : UObjectExporter.CallNativeGetName(NativeObject);
    
    
    /// <summary>
    /// Whether the object has been destroyed.
    /// </summary>
    public bool IsDestroyed => NativeObject == IntPtr.Zero || !UObjectExporter.CallNativeIsValid(NativeObject);

    /// <inheritdoc />
    public override string ToString()
    {
        return ObjectName.ToString();
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
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
    public void PrintString(string message = "Hello", float duration = 2.0f, LinearColor color = default, bool printToScreen = true, bool printToConsole = true)
    {
        unsafe
        {
            fixed (char* messagePtr = message)
            {
                // Use the default color if none is provided
                if (color.IsZero())
                {
                    color = new LinearColor
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
    public static T NewObject<T>(CoreUObject.Object outer, SubclassOf<T> classType = default, CoreUObject.Object template = null) where T : UnrealSharpObject
    {
        if (classType.NativeClass == IntPtr.Zero)
        {
            classType = new SubclassOf<T>();
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
    public static Package? GetTransientPackage()
    {
        IntPtr handle = UObjectExporter.CallGetTransientPackage();
        return GcHandleUtilities.GetObjectFromHandlePtr<Package>(handle);
    }
    
    /// <summary>
    /// Get the default object of the specified type.
    /// </summary>
    /// <typeparam name="T"> The type of the object to get. </typeparam>
    /// <returns> The default object of the specified type. </returns>
    public static T GetDefault<T>() where T : CoreUObject.Object
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
    public static T GetDefault<T>(CoreUObject.Object obj) where T : UnrealSharpObject
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
    public T SpawnActor<T>(SubclassOf<T> actorType = default, 
        Transform spawnTransform = default,
        ESpawnActorCollisionHandlingMethod spawnMethod = ESpawnActorCollisionHandlingMethod.Undefined, 
        Pawn? instigator = null, 
        Actor? owner = null) where T : Actor
    {
        ActorSpawnParameters actorSpawnParameters = new ActorSpawnParameters
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
    public T SpawnActor<T>(Transform spawnTransform, SubclassOf<T> actorType, ActorSpawnParameters spawnParameters) where T : Actor
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
    public T GetWorldSubsystem<T>() where T : WorldSubsystem
    {
        var subsystemClass = new SubclassOf<T>(typeof(T));
        IntPtr handle = UWorldExporter.CallGetWorldSubsystem(subsystemClass.NativeClass, NativeObject);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }
    
    /// <summary>
    /// Gets the game instance subsystem of the specified type.
    /// </summary>
    /// <typeparam name="T"> The type of the subsystem to get. </typeparam>
    /// <returns> The game instance subsystem of the specified type. </returns>
    public T GetGameInstanceSubsystem<T>() where T : GameInstanceSubsystem
    {
        var subsystemClass = new SubclassOf<T>(typeof(T));
        IntPtr handle = UGameInstanceExporter.CallGetGameInstanceSubsystem(subsystemClass.NativeClass, NativeObject);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }
    
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
    
    /// <summary>
    /// Gets the engine subsystem of the specified type.
    /// </summary>
    /// <typeparam name="T"> The type of the subsystem to get. </typeparam>
    /// <returns> The engine subsystem of the specified type. </returns>
    public T GetEngineSubsystem<T>() where T : EngineSubsystem
    {
        var subsystemClass = new SubclassOf<T>(typeof(T));
        IntPtr handle = GEngineExporter.CallGetEngineSubsystem(subsystemClass.NativeClass);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }
    
    /// <summary>
    /// Gets the local player subsystem of the specified type.
    /// </summary>
    /// <param name="playerController"> The player controller to get the subsystem from. </param>
    /// <typeparam name="T"> The type of the subsystem to get. </typeparam>
    /// <returns> The local player subsystem of the specified type. </returns>
    public T GetLocalPlayerSubsystem<T>(PlayerController playerController) where T : LocalPlayerSubsystem
    {
        var subsystemClass = new SubclassOf<T>(typeof(T));
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
    public T CreateWidget<T>(SubclassOf<T> widgetClass, PlayerController? owningController = null) where T : UserWidget
    {
        unsafe
        {
            IntPtr owningPlayerPtr = owningController?.NativeObject ?? IntPtr.Zero;
            IntPtr handle = UWidgetBlueprintLibraryExporter.CreateWidget(NativeObject, widgetClass.NativeClass, owningPlayerPtr);
            return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
        }
    }
    
    /// <summary>
    /// Checks if the object is valid.
    /// </summary>
    /// <exception cref="UnrealObjectDestroyedException"> Thrown if the object is not valid. </exception>
    protected void CheckObjectForValidity()
    {
        if (!UObjectExporter.CallNativeIsValid(NativeObject))
        {
            throw new UnrealObjectDestroyedException($"{this} is not valid or pending kill.");
        }
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        NativeObject = IntPtr.Zero;
        GC.SuppressFinalize(this);
    }
}
