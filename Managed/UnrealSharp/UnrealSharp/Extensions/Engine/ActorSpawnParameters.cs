using System.Runtime.InteropServices;
using UnrealSharp.CoreUObject;

namespace UnrealSharp.Engine;

[StructLayout(LayoutKind.Sequential)]
public struct FActorSpawnParameters()
{
    private IntPtr ownerNativePtr = 0;
    private IntPtr instigatorNativePtr = 0;
    private IntPtr templateNativePtr = 0;
    private NativeBool nativeDeferConstruction = NativeBool.False;
    public ESpawnActorCollisionHandlingMethod SpawnMethod = ESpawnActorCollisionHandlingMethod.Undefined;

    private AActor? _owner = null; 
    
    public AActor? Owner
    { 
        get => _owner;
        set
        {
            _owner = value;
            ownerNativePtr = _owner?.NativeObject ?? IntPtr.Zero;
        }
    }

    private APawn? _instigator = null;

    public APawn? Instigator
    { 
        get => _instigator;
        set
        {
            _instigator = value;
            instigatorNativePtr = _instigator?.NativeObject ?? IntPtr.Zero;
        }
    }
    
    private UObject? _template = null;
    
    public UObject? Template
    { 
        get => _template;
        set
        {
            _template = value;
            templateNativePtr = _template?.NativeObject ?? IntPtr.Zero;
        }
    }

    public bool DeferConstruction
    {
        set => nativeDeferConstruction = value ? NativeBool.True : NativeBool.False;
        get => nativeDeferConstruction == NativeBool.True;
    }
}
