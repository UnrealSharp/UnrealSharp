using UnrealSharp.Interop;

namespace UnrealSharp;

public class EventDispatcher(IntPtr nativeDelegate, UnrealSharpObject owner)
{
    public void Clear()
    {
        FMulticastDelegatePropertyExporter.CallClearDelegate(NativeDelegate, Owner.NativeObject);
    }

    protected UnrealSharpObject Owner = owner;
    protected IntPtr NativeDelegate = nativeDelegate;
}