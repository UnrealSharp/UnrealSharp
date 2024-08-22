using System.Runtime.InteropServices;
using UnrealSharp.Attributes;
using UnrealSharp.Interop;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct FTextData
{
    public IntPtr ObjectPointer;
    public uint Flags;
}

[Binding]
public struct FText
{
    internal FTextData Data;
    
    public bool Empty => Data.ObjectPointer == IntPtr.Zero;
    public static FText None = new();
    
    public FText()
    {
        FTextExporter.CallCreateEmptyText(ref Data);
    }
    
    public FText(string text)
    {
        FTextExporter.CallFromString(ref Data, text);
    }
    
    public FText(FName name) : this(name.ToString())
    {
        
    }
    
    internal FText(FTextData nativeInstance)
    {
        Data = nativeInstance;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == this.GetType() && Equals((FText)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Data.GetHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        unsafe
        {
            return new string(FTextExporter.CallToString(ref Data));
        }
    }
    
    public static implicit operator FText(string value)
    {
        return new FText(value);
    }
    
    public static implicit operator string(FText value)
    {
        return value.ToString();
    }
    
    public static bool operator ==(FText a, FText b)
    {
        return a.Data.ObjectPointer == b.Data.ObjectPointer;
    }

    public static bool operator !=(FText a, FText b)
    {
        return !(a == b);
    }
}

public static class TextMarshaller
{ 
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, FText obj)
    {
        BlittableMarshaller<FTextData>.ToNative(nativeBuffer, arrayIndex, obj.Data);
    }
    public static FText FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        FText data = new FText(BlittableMarshaller<FTextData>.FromNative(nativeBuffer, arrayIndex));
        return data;
    }
}