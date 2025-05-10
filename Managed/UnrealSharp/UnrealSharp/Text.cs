using System.Runtime.InteropServices;
using UnrealSharp.Attributes;
using UnrealSharp.Core.Attributes;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.Interop;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct FTextData
{
    public FSharedPtr ObjectPointer;
    public uint Flags;
}

[Binding]
public class FText
{
    internal FTextData Data;

    public bool Empty => !Data.ObjectPointer.Valid;
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
        Data.ObjectPointer.AddRef();
    }
    
    ~FText()
    {
        if (Empty)
        {
            return;
        }
        
        Data.ObjectPointer.Release();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((FText)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Data.GetHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (Empty)
        {
            return string.Empty;
        }
        
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
        unsafe
        {
            FTextData* to = (FTextData*)(nativeBuffer + arrayIndex * sizeof(FTextData));
            to->ObjectPointer.Release();
            *to = obj.Data;
            to->ObjectPointer.AddRef();
        }
    }
    public static FText FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        FText data = new FText(BlittableMarshaller<FTextData>.FromNative(nativeBuffer, arrayIndex));
        return data;
    }
}