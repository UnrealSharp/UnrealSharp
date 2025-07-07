using System.Runtime.InteropServices;
using UnrealSharp.Core.Interop;

namespace UnrealSharp.Core.Marshallers;

public class ManagedObjectMarshaller<T>
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, T? obj)
    {
        GCHandle handle = obj is not null ? GCHandle.Alloc(obj, GCHandleType.Normal) : GCHandle.FromIntPtr(0);
        ManagedHandleExporter.CallStoreManagedHandle(GCHandle.ToIntPtr(handle), nativeBuffer);
    }
    
    public static T? FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        if (nativeBuffer == IntPtr.Zero)
        {
            return default!;
        }
        
        IntPtr handle = ManagedHandleExporter.CallLoadManagedHandle(nativeBuffer);
        return GCHandleUtilities.GetObjectFromHandlePtr<T>(handle);
    }
}