using UnrealSharp.Core;
using UnrealSharp.CoreUObject;
using UnrealSharp.Interop;

namespace UnrealSharp;

public abstract class Delegate<TDelegate> : DelegateBase<TDelegate> where TDelegate : Delegate
{
    private IntPtr _nativeDelegate;
    private TWeakObjectPtr<UObject> _targetObject;
    private FName _functionName;

    public TWeakObjectPtr<UObject> TargetObject
    {
        get
        {
            if (_nativeDelegate == IntPtr.Zero)
            {
                return _targetObject;
            }

            Bind_FScriptDelegate.CallGetDelegateInfo(_nativeDelegate, out IntPtr targetObjectPtr, out _);
            return new TWeakObjectPtr<UObject>(targetObjectPtr);
        }
        set
        {
            if (_nativeDelegate == IntPtr.Zero)
            {
                _targetObject = value;
                return;
            }

            UObject? targetObject = value.Object;
            Bind_FScriptDelegate.CallMakeDelegate(_nativeDelegate, targetObject?.NativeObject ?? IntPtr.Zero, FunctionName);
        }
    }

    public FName FunctionName
    {
        get
        {
            if (_nativeDelegate == IntPtr.Zero)
            {
                return _functionName;
            }

            Bind_FScriptDelegate.CallGetDelegateInfo(_nativeDelegate, out _, out FName functionName);
            return functionName;
        }
        set
        {
            if (_nativeDelegate == IntPtr.Zero)
            {
                _functionName = value;
                return;
            }

            Bind_FScriptDelegate.CallGetDelegateInfo(_nativeDelegate, out IntPtr targetObjectPtr, out _);
            Bind_FScriptDelegate.CallMakeDelegate(_nativeDelegate, targetObjectPtr, value);
        }
    }
    
    public Delegate()
    {
        
    }
    
    public Delegate(UObject targetObject, FName functionName)
    {
        _targetObject = new TWeakObjectPtr<UObject>(targetObject);
        _functionName = functionName;
    }

    public override void FromNative(IntPtr address, IntPtr nativeProperty)
    {
        _nativeDelegate = nativeProperty != IntPtr.Zero ? address : IntPtr.Zero;
        if (_nativeDelegate != IntPtr.Zero)
        {
            return;
        }

        Bind_FScriptDelegate.CallGetDelegateInfo(address, out IntPtr targetObjectPtr, out FName functionName);
        _targetObject = new TWeakObjectPtr<UObject>(targetObjectPtr);
        _functionName = functionName;
    }

    public override void ToNative(IntPtr address)
    {
        if (_nativeDelegate != IntPtr.Zero)
        {
            Bind_FScriptDelegate.CallCopyDelegate(address, _nativeDelegate);
            return;
        }

        UObject? targetObject = _targetObject.Object;
        Bind_FScriptDelegate.CallMakeDelegate(address, targetObject?.NativeObject ?? IntPtr.Zero, _functionName);
    }

    public override bool Contains(TDelegate handler)
    {
        if (handler.Target is not UObject targetObject)
        {
            return false;
        }

        if (_nativeDelegate != IntPtr.Zero)
        {
            return Bind_FScriptDelegate.CallContainsDelegate(_nativeDelegate, targetObject.NativeObject, handler.Method.Name).ToManagedBool();
        }

        return targetObject.Equals(_targetObject.Object) && _functionName == handler.Method.Name;
    }
    
    public override void BindUFunction(UObject targetObject, FName functionName)
    {
        if (_nativeDelegate != IntPtr.Zero)
        {
            Bind_FScriptDelegate.CallMakeDelegate(_nativeDelegate, targetObject.NativeObject, functionName);
            return;
        }

        _targetObject = new TWeakObjectPtr<UObject>(targetObject);
        _functionName = functionName;
    }

    public override void BindUFunction(TWeakObjectPtr<UObject> targetObjectPtr, FName functionName)
    {
        if (_nativeDelegate != IntPtr.Zero)
        {
            UObject? targetObject = targetObjectPtr.Object;
            Bind_FScriptDelegate.CallMakeDelegate(_nativeDelegate, targetObject?.NativeObject ?? IntPtr.Zero, functionName);
            return;
        }

        _targetObject = targetObjectPtr;
        _functionName = functionName;
    }

    public override void Add(TDelegate handler)
    {
        if (IsBound)
        {
            throw new InvalidOperationException($"A singlecast delegate can only be bound to one handler at a time. Unbind it first before binding a new handler.");
        }
        
        if (handler.Target is not UObject targetObject)
        {
            throw new ArgumentException("The callback for a singlecast delegate must be a valid UFunction defined on a UClass", nameof(handler));
        }
        
        BindUFunction(targetObject, new FName(handler.Method.Name));
    }

    public override void Remove(TDelegate handler)
    {
        if (!Contains(handler))
        {
            return;
        }
        
        Clear();
    }

    public override bool IsBound => _nativeDelegate != IntPtr.Zero
        ? Bind_FScriptDelegate.CallIsBound(_nativeDelegate).ToManagedBool()
        : _targetObject.IsValid && !_functionName.IsNone;

    public override void Clear()
    {
        if (_nativeDelegate != IntPtr.Zero)
        {
            Bind_FScriptDelegate.CallClearDelegate(_nativeDelegate);
            return;
        }

        _targetObject = new TWeakObjectPtr<UObject>();
        _functionName = FName.None;
    }

    public override string ToString()
    {
        if (_nativeDelegate != IntPtr.Zero)
        {
            Bind_FScriptDelegate.CallGetDelegateInfo(_nativeDelegate, out IntPtr targetObjectPtr, out FName functionName);
            return $"{new TWeakObjectPtr<UObject>(targetObjectPtr).Object}::{functionName}";
        }

        return $"{_targetObject.Object}::{_functionName}";
    }

    protected override void ProcessDelegate(IntPtr parameters)
    {
        if (_nativeDelegate != IntPtr.Zero)
        {
            Bind_FScriptDelegate.CallProcessDelegate(_nativeDelegate, parameters);
            return;
        }

        UObject? targetObject = _targetObject.Object;
        
        if (targetObject == null)
        {
            LogUnrealSharp.LogWarning($"Attempted to invoke delegate, but target object is null. Delegate: {this}");
            return;
        }
        
        Bind_FScriptDelegate.CallBroadcastDelegate(targetObject.NativeObject, _functionName, parameters);
    }
}
