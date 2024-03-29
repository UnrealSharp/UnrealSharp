using System.Runtime.InteropServices;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct ActorSpawnParameters()
{
    private IntPtr ownerNativePtr = 0;
    private IntPtr instigatorNativePtr = 0;
    private NativeBool nativeDeferConstruction = NativeBool.False;

    public SpawnActorCollisionHandlingMethod SpawnMethod = SpawnActorCollisionHandlingMethod.Default;

    private UnrealSharpObject _owner = null; 
    public UnrealSharpObject Owner
    { 
        get => _owner;
        set
        {
            _owner = value;
            ownerNativePtr = _owner?.NativeObject ?? IntPtr.Zero;
        }
    }

    private UnrealSharpObject _instigator = null;

    public UnrealSharpObject Instigator
    { 
        get => _instigator;
        set
        {
            _instigator = value;
            instigatorNativePtr = _instigator?.NativeObject ?? IntPtr.Zero;
        }
    }

    public bool DeferConstruction
    {
        set => nativeDeferConstruction = value ? NativeBool.True : NativeBool.False;
        get => nativeDeferConstruction == NativeBool.True;
    }
}
