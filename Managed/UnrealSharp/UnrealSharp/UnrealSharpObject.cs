using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnrealSharp.CoreUObject;
using UnrealSharp.CSharpForUE;
using UnrealSharp.Engine;
using UnrealSharp.Interop;
using UnrealSharp.UMG;
using UnrealSharp.UnrealSharpEditor;

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

public class UnrealSharpObject() : IDisposable
{
    public IntPtr NativeObject { get; private set; }
    public Name ObjectName => IsDestroyed ? Name.None : UObjectExporter.CallNativeGetName(NativeObject);
    public bool IsDestroyed => NativeObject == IntPtr.Zero || !UObjectExporter.CallNativeIsValid(NativeObject);
    
    internal static IntPtr Create(Type typeToCreate, IntPtr nativeObjectPtr)
    {
        UnrealSharpObject createdObject = (UnrealSharpObject) RuntimeHelpers.GetUninitializedObject(typeToCreate);

        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var foundConstructor = typeToCreate.GetConstructor(bindingFlags, null, Type.EmptyTypes, null);

        createdObject.NativeObject = nativeObjectPtr;
        
        foundConstructor.Invoke(createdObject, null);

        return GCHandle.ToIntPtr(GcHandleUtilities.AllocateStrongPointer(createdObject));
    }

    public override string ToString()
    {
        return ObjectName.ToString();
    }

    public override bool Equals(object obj)
    {
        return obj is UnrealSharpObject unrealSharpObject && NativeObject == unrealSharpObject.NativeObject;
    }

    public override int GetHashCode()
    {
        return NativeObject.GetHashCode();
    }
    
    public void PrintString(string message = "Hello", float duration = 2.0f, LinearColor color = default, bool printToScreen = true, bool printToConsole = true)
    {
        unsafe
        {
            fixed (char* messagePtr = message)
            {
                // Use the default color if none is provided
                if (color.IsZero())
                {
                    color = new LinearColor(0.0f, 0.66f, 1);
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
    
    public void PrintToConsole(string message = "Hello")
    {
        PrintString(message, printToScreen: false);
    }
    
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
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }

    public static Package? GetTransientPackage()
    {
        IntPtr handle = UObjectExporter.CallGetTransientPackage();
        return GcHandleUtilities.GetObjectFromHandlePtr<Package>(handle);
    }
    
    public static T GetDefault<T>() where T : CoreUObject.Object
    {
        IntPtr handle = UClassExporter.CallGetDefaultFromString(typeof(T).Name);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }
    
    public static T GetDefault<T>(CoreUObject.Object obj) where T : UnrealSharpObject
    {
        IntPtr handle = UClassExporter.CallGetDefaultFromInstance(obj.NativeObject);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }
    
    public T SpawnActor<T>(Transform spawnTransform, 
        SubclassOf<T> actorType, 
        SpawnActorCollisionHandlingMethod spawnMethod = SpawnActorCollisionHandlingMethod.Default, 
        UnrealSharpObject instigator = null, 
        UnrealSharpObject owner = null) where T : Actor
    {
        ActorSpawnParameters actorSpawnParameters = new ActorSpawnParameters
        {
            Instigator = instigator,
            DeferConstruction = false,
            Owner = owner,
            SpawnMethod = spawnMethod,
        };
        
        IntPtr handle = UWorldExporter.CallSpawnActor(NativeObject, spawnTransform, actorType.NativeClass, actorSpawnParameters);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }
    
    public T SpawnActor<T>(Transform spawnTransform, 
        SubclassOf<T> actorType, 
        ActorSpawnParameters spawnParameters) where T : Actor
    {
        return SpawnActor(spawnTransform, actorType, spawnParameters);
    }
    
    public T GetWorldSubsystem<T>() where T : CSWorldSubsystem
    {
        var subsystemClass = new SubclassOf<T>(typeof(T));
        IntPtr handle = UWorldExporter.CallGetWorldSubsystem(subsystemClass.NativeClass, NativeObject);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }
    
    public T GetGameInstanceSubsystem<T>() where T : CSGameInstanceSubsystem
    {
        var subsystemClass = new SubclassOf<T>(typeof(T));
        IntPtr handle = UGameInstanceExporter.CallGetGameInstanceSubsystem(subsystemClass.NativeClass, NativeObject);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }
    
    public static T GetEditorSubsystem<T>() where T : CSEditorSubsystem
    {
        var subsystemClass = new SubclassOf<T>(typeof(T));
        IntPtr handle = GEditorExporter.CallGetEditorSubsystem(subsystemClass.NativeClass);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }
    
    public T GetEngineSubsystem<T>() where T : CSEngineSubsystem
    {
        var subsystemClass = new SubclassOf<T>(typeof(T));
        IntPtr handle = GEngineExporter.CallGetEngineSubsystem(subsystemClass.NativeClass);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }
    
    public T GetLocalPlayerSubsystem<T>(PlayerController playerController) where T : CSLocalPlayerSubsystem
    {
        if (playerController == null)
        {
            return null;
        }

        var subsystemClass = new SubclassOf<T>(typeof(T));
        IntPtr handle = ULocalPlayerExporter.CallGetLocalPlayerSubsystem(subsystemClass.NativeClass, playerController.NativeObject);
        return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }
    
    public T CreateWidget<T>(SubclassOf<T> widgetClass, PlayerController owningController = null) where T : UserWidget
    {
        unsafe
        {
            IntPtr owningPlayerPtr = owningController != null ? owningController.NativeObject : IntPtr.Zero;
            IntPtr handle = UWidgetBlueprintLibraryExporter.CreateWidget(NativeObject, widgetClass.NativeClass, owningPlayerPtr);
            return GcHandleUtilities.GetObjectFromHandlePtr<T>(handle);
        }
    }
    
    public static TimerHandle SetTimer(Action action, float duration, bool loop)
    {
        unsafe
        {
            if (action.Target == null)
            {
                return default;
            }

            if (action.Target is not UnrealSharpObject owner)
            {
                throw new ArgumentException("The target of the action must be an UnrealSharpObject.");
            }
        
            TimerHandle handle = new TimerHandle();
            UWorldExporter.CallSetTimer(owner.NativeObject, action.Method.Name, duration, loop.ToNativeBool(), &handle);
            return handle;
        }
    }
    
    public static void InvalidateTimer(CoreUObject.Object worldContextObject, TimerHandle handle)
    {
        unsafe
        {
            UWorldExporter.CallInvalidateTimer(worldContextObject.NativeObject, &handle);
        }
    }
    
    protected void CheckObjectForValidity()
    {
        if (!UObjectExporter.CallNativeIsValid(NativeObject))
        {
            throw new UnrealObjectDestroyedException($"{this} is not valid or pending kill.");
        }
    }
    
    public virtual void Dispose()
    {
        NativeObject = IntPtr.Zero;
        GC.SuppressFinalize(this);
    }
}
