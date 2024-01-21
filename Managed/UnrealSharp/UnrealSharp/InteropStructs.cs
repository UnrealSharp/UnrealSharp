namespace UnrealSharp;

// Bools are not blittable, so we need to convert them to bytes
public enum NativeBool : byte
{
    True = 1,
    False = 0
}

public static class BoolConverter
{ 
    public static NativeBool ToNativeBool(this bool value)
    {
        return value ? NativeBool.True : NativeBool.False;
    }
    
    public static bool ToManagedBool(this NativeBool value)
    {
        byte byteValue = (byte) value;
        return byteValue != 0;
    }
}