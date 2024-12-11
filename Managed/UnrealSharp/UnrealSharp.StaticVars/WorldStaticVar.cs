using System.Diagnostics;
using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp.StaticVars;

/// <summary>
/// A static variable that has the lifetime of a UWorld. When the world is destroyed, the value is destroyed.
/// For example when traveling between levels, the value is destroyed.
/// </summary>
public class FWorldStaticVar<T> : FBaseStaticVar<T>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly Dictionary<IntPtr, T> _worldToValue = new Dictionary<IntPtr, T>();
    
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly FWorldDelegates.FWorldCleanupEvent _onWorldCleanupDelegate;
    
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly FDelegateHandle _onWorldCleanupHandle;
    
    public FWorldStaticVar()
    {
        _onWorldCleanupDelegate = OnWorldCleanup;
        IntPtr onWorldCleanup = Marshal.GetFunctionPointerForDelegate(_onWorldCleanupDelegate);
        FWorldDelegatesExporter.CallBindOnWorldCleanup(onWorldCleanup, out _onWorldCleanupHandle);
    }
    
    public FWorldStaticVar(T value) : this()
    {
        Value = value;
    }
    
    ~FWorldStaticVar()
    {
        FWorldDelegatesExporter.CallUnbindOnWorldCleanup(_onWorldCleanupHandle);
    }
    
    public sealed override T? Value
    {
        get => GetWorldValue();
        set => SetWorldValue(value!);
    }
    
    private T? GetWorldValue()
    {
        IntPtr worldPtr = FCSManagerExporter.CallGetCurrentWorldPtr();
        return _worldToValue.GetValueOrDefault(worldPtr);
    }
    
    private void SetWorldValue(T value)
    {
        IntPtr worldPtr = FCSManagerExporter.CallGetCurrentWorldPtr();
        if (_worldToValue.TryAdd(worldPtr, value))
        {
            return;
        }
        
        _worldToValue[worldPtr] = value;
    }
    
    private void OnWorldCleanup(IntPtr world, NativeBool sessionEnded, NativeBool cleanupResources)
    {
        _worldToValue.Remove(world);
    }
}