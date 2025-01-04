using System.Diagnostics;
using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp.StaticVars;

/// <summary>
/// A static variable which will be alive during the whole game session.
/// In editor the value will reset on Play In Editor start/end and on hot reload.
/// </summary>
/// <typeparam name="T">The type of the static variable</typeparam>
public sealed class FGameStaticVar<T> : FBaseStaticVar<T>
{
#if !PACKAGE
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly FDelegateHandle _onPieStartEndHandle;
    
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly FDelegateHandle _onPieEndHandle;
    
    public FGameStaticVar()
    {
        FEditorDelegates.FOnPIEEvent onPIEndDelegate = OnPIEStartEnd;
        IntPtr onPIEStartEndFuncPtr = Marshal.GetFunctionPointerForDelegate(onPIEndDelegate);
        FEditorDelegatesExporter.CallBindEndPIE(onPIEStartEndFuncPtr, out _onPieEndHandle);
        FEditorDelegatesExporter.CallBindStartPIE(onPIEStartEndFuncPtr, out _onPieStartEndHandle);
    }
    
    public FGameStaticVar(T value) : this()
    {
        Value = value;
    }
    
    private void OnPIEStartEnd(NativeBool simulating)
    {
        ResetToDefault();
    }
    
    ~FGameStaticVar()
    {
        ResetToDefault();
        
        FEditorDelegatesExporter.CallUnbindStartPIE(_onPieStartEndHandle);
        FEditorDelegatesExporter.CallUnbindEndPIE(_onPieEndHandle);
    }
    
    void ResetToDefault()
    {
        Value = default;
    }
    
#else
    public FGameStaticVar(T value)
    {
        Value = value;
    }
#endif
    
    public static implicit operator T(FGameStaticVar<T> value)
    {
        return value.Value;
    }
}