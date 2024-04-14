using System.Runtime.InteropServices;
using UnrealSharp.Engine;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct ActorSpawnParameters()
{
    private IntPtr ownerNativePtr = 0;
    private IntPtr instigatorNativePtr = 0;
    private IntPtr templateNativePtr = 0;
    private NativeBool nativeDeferConstruction = NativeBool.False;
    public ESpawnActorCollisionHandlingMethod SpawnMethod = ESpawnActorCollisionHandlingMethod.Undefined;

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
    
    private UnrealSharpObject _template = null;
    
    public UnrealSharpObject Template
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
