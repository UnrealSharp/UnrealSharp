using System.Diagnostics;
using System.Runtime.InteropServices;
using UnrealSharp.Attributes;
using UnrealSharp.Core;
using UnrealSharp.Core.Attributes;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.CoreUObject;
using UnrealSharp.Engine;
using UnrealSharp.Interop;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct FStrongObjectPtr
{
    internal IntPtr NativeObject;
}

public abstract class TStrongObjectPtr : IEquatable<TStrongObjectPtr>, IDisposable
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private FStrongObjectPtr _nativePtr;

    private bool _isDisposed;

    protected TStrongObjectPtr(UObject? obj = null)
    {
        TStrongObjectPtrExporter.CallConstructStrongObjectPtr(ref _nativePtr, obj?.NativeObject ?? IntPtr.Zero);
    }

    ~TStrongObjectPtr()
    {
        Dispose();
    }
    
    public bool IsValid => !_isDisposed && _nativePtr.NativeObject != IntPtr.Zero;
    public UObject? Value
    {
        get
        {
            if (!IsValid)
            {
                return null;
            }
            
            IntPtr handle = FCSManagerExporter.CallFindManagedObject(_nativePtr.NativeObject);
            return GCHandleUtilities.GetObjectFromHandlePtr<UObject>(handle);
        }
    }

    public bool Equals(TStrongObjectPtr? other)
    {
        if (other is null)
        {
            return _nativePtr.NativeObject == IntPtr.Zero;
        }
        
        return _nativePtr.NativeObject == other._nativePtr.NativeObject;
    }
    
    public override bool Equals(object? obj)
    {
        return obj is TStrongObjectPtr ptr && Equals(ptr);
    }

    public static bool operator ==(TStrongObjectPtr? a, TStrongObjectPtr? b)
    {
        return a?.Equals(b) ?? b is null;
    }

    public static bool operator !=(TStrongObjectPtr? a, TStrongObjectPtr? b)
    {
        return !(a == b);
    }

    public override int GetHashCode()
    {
        return _nativePtr.NativeObject.GetHashCode();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        
        TStrongObjectPtrExporter.CallDestroyStrongObjectPtr(ref _nativePtr);
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}

[Binding]
public sealed class TStrongObjectPtr<T>(T? obj = null) : TStrongObjectPtr(obj) where T : UObject
{
    public new T? Value => (T?) base.Value;
    
    public static implicit operator TStrongObjectPtr<T>(T? obj) => new(obj);
}