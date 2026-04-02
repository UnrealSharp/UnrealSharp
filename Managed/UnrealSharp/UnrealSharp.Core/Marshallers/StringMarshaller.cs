using UnrealSharp.Core.Interop;

namespace UnrealSharp.Core.Marshallers;

public static class StringMarshaller
{
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, string stringToMarshal)
    {
        unsafe
        {
            if (string.IsNullOrEmpty(stringToMarshal)) 
            {
                //Guard against C# null strings (use string.Empty instead)
                stringToMarshal = string.Empty; 
            }
            
            UnmanagedArray* unrealString = (UnmanagedArray*) (nativeBuffer + arrayIndex * sizeof(UnmanagedArray));

            // NOTE: do not pass a string directly to native (the runtime marshals it as ANSI and replaces non-ASCII with '?').
            // Pin the UTF-16 buffer and let the UE side convert it to TCHAR/FString.
            fixed (char* stringPtr = stringToMarshal)
            {
                FStringExporter.CallMarshalToNativeStringView(unrealString, stringPtr, stringToMarshal.Length);
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
