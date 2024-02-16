using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct TextData
{
    public IntPtr ObjectPointer;
    public int SharedReferenceCount;
    public int WeakReferenceCount;
    public uint Flags;
}

public class Text
{
    internal TextData Data;
    
    public bool Empty => Data.ObjectPointer == IntPtr.Zero;
    public static Text None = new();
    
    public Text()
    {
        FTextExporter.CallCreateEmptyText(ref Data);
    }
    
    public Text(string text)
    {
        FTextExporter.CallFromString(ref Data, text);
    }
    
    public Text(Name name) : this(name.ToString())
    {
        
    }
    
    internal Text(TextData nativeInstance)
    {
        Data = nativeInstance;
    }
    
    protected bool Equals(Text other)
    {
        return Data.Equals(other.Data);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == this.GetType() && Equals((Text)obj);
    }

    public override int GetHashCode()
    {
        return Data.GetHashCode();
    }
    
    public override string ToString()
    {
        unsafe
        {
            return new string(FTextExporter.CallToString(ref Data));
        }
    }
    
    public static implicit operator Text(string value)
    {
        return new Text(value);
    }
    
    public static implicit operator string(Text value)
    {
        return value.ToString();
    }
    
    public static bool operator ==(Text a, Text b)
    {
        if (a is null || b is null)
        {
            return false;
        }
        
        return a.Data.ObjectPointer == b.Data.ObjectPointer;
    }

    public static bool operator !=(Text a, Text b)
    {
        return !(a == b);
    }
}

public static class TextMarshaller
{ 
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner, Text obj)
    {
        BlittableMarshaller<TextData>.ToNative(nativeBuffer, arrayIndex, owner, obj.Data);
    }
    public static Text FromNative(IntPtr nativeBuffer, int arrayIndex, UnrealSharpObject owner)
    {
        Text data = new Text(BlittableMarshaller<TextData>.FromNative(nativeBuffer, arrayIndex, owner));
        return data;
    }
}