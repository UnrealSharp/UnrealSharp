using System.Runtime.InteropServices;
using UnrealSharp.Core;

namespace UnrealSharp.UnrealSharpAsync;

public static class NativeAsyncUtilities
{
    public static void InitializeAsyncAction(UCSAsyncActionBase action, Action managedCallback)
    {
        GCHandle callbackHandle = GCHandleUtilities.AllocateWeakPointer(managedCallback);
        UCSAsyncBaseExporter.CallInitializeAsyncObject(action.NativeObject, GCHandle.ToIntPtr(callbackHandle));
    }
}