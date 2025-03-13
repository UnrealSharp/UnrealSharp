namespace UnrealSharp.Core.Marshallers;

public static class BitfieldBoolMarshaller
{
    private const int BoolSize = sizeof(NativeBool);
    
    public static void ToNative(IntPtr valuePtr, byte fieldMask, bool value)
    {
        unsafe
        {
            var byteValue = (byte*)valuePtr;
            var mask = value ? fieldMask : byte.MinValue;
            *byteValue = (byte)((*byteValue & ~fieldMask) | mask);
        }
    }

    public static bool FromNative(IntPtr valuePtr, byte fieldMask)
    {
        unsafe
        {
            var byteValue = (byte*)valuePtr;
            return (*byteValue & fieldMask) != 0;
        }
    }
}