using System.Runtime.InteropServices;
using UnrealSharp.Core.Attributes;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.Interop;

namespace UnrealSharp.Core;

[StructLayout(LayoutKind.Sequential)]
public struct FTextData
{
    public FSharedPtr ObjectPointer;
    public uint Flags;
}

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
        if (text == null)
        {
            FTextExporter.CallCreateEmptyText(ref Data);
            return;
        }

        // NOTE: do not pass a string directly to native (the runtime marshals it as ANSI and replaces non-ASCII with '?').
        // Pin the UTF-16 buffer and let the UE side build FText from a TCHAR view (pointer + length).
        unsafe
        {
            fixed (char* textPtr = text)
            {
                FTextExporter.CallFromStringView(ref Data, textPtr, text.Length);
            }
        }
    }

    public FText(ReadOnlySpan<char> text)
    {
        unsafe
        {
            fixed (char* textPtr = text)
            {
                FTextExporter.CallFromStringView(ref Data, textPtr, text.Length);
            }
        }
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
        return obj.GetType() == GetType() && this == (FText)obj;
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

    public ReadOnlySpan<char> AsReadOnlySpan()
    {
        if (Empty)
        {
            return ReadOnlySpan<char>.Empty;
        }

        unsafe
        {
            FTextExporter.CallToStringView(ref Data, out char* textPtr, out int length);
            return new ReadOnlySpan<char>(textPtr, length);
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
    
    public static implicit operator ReadOnlySpan<char>(FText value)
    {
        return value.AsReadOnlySpan();
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
