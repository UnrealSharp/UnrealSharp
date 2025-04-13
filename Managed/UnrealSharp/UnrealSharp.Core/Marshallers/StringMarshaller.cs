using UnrealSharp.Interop;

namespace UnrealSharp.Core.Marshallers;

public static class StringMarshaller
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, string obj)
    {
        unsafe
        {
            if (string.IsNullOrEmpty(obj)) 
            {
                //Guard against C# null strings (use string.Empty instead)
                obj = string.Empty; 
            }
            
            IntPtr unrealString = nativeBuffer + arrayIndex * sizeof(UnmanagedArray);
            
            fixed (char* stringPtr = obj)
            {
                FStringExporter.CallMarshalToNativeString(unrealString, stringPtr);
            }
        }
    }
    
    public static string FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        unsafe
        {
            UnmanagedArray unrealString = BlittableMarshaller<UnmanagedArray>.FromNative(nativeBuffer, arrayIndex);
            return unrealString.Data == IntPtr.Zero ? string.Empty : new string((char*) unrealString.Data);
        }
    }
    
    public static void DestructInstance(IntPtr nativeBuffer, int arrayIndex)
    {
        unsafe
        {
            UnmanagedArray* unrealString = (UnmanagedArray*) (nativeBuffer + arrayIndex * sizeof(UnmanagedArray));
            unrealString->Destroy();
        }
    }
}