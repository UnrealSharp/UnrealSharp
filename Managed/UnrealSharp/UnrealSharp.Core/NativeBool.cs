namespace UnrealSharp.Core;

// C++ bool is 1 byte. With runtime marshalling on, a C# bool in a P/Invoke or delegate* unmanaged
// signature is marshalled as a 4-byte Win32 BOOL, so a native false can read back as true.
// Use NativeBool for native bool in those signatures; analyzer US0015 enforces it.
public enum NativeBool : byte
{
    False = 0,
    True = 1
}

public static class BoolConverter
{ 
    public static NativeBool ToNativeBool(this bool value) => value ? NativeBool.True : NativeBool.False;
    public static bool ToManagedBool(this NativeBool value) => (byte) value != 0;
}
