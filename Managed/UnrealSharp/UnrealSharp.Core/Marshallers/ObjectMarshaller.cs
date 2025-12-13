using UnrealSharp.Core.Interop;

namespace UnrealSharp.Core.Marshallers;

public static class ObjectMarshaller<T> where T : UnrealSharpObject
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, T? obj)
    {
        IntPtr nativeTObjectPtr = nativeBuffer + arrayIndex * IntPtr.Size;
        TObjectPtrExporter.CallSetTObjectPtrPropertyValue(nativeTObjectPtr, obj?.NativeObject ?? IntPtr.Zero);
    }
    
    public static T FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        IntPtr uObjectPointer = BlittableMarshaller<IntPtr>.FromNative(nativeBuffer, arrayIndex);
        
        if (uObjectPointer == IntPtr.Zero)
        {
            return null!;
        }
        
        IntPtr handle = FCSManagerExporter.CallFindManagedObject(uObjectPointer);
        return GCHandleUtilities.GetObjectFromHandlePtr<T>(handle)!;
    }
}
