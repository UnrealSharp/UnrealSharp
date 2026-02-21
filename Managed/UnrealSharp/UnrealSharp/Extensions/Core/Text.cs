using System.Runtime.InteropServices;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.Interop;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.Core;

[StructLayout(LayoutKind.Sequential)]
public struct FTextData
{
    public FSharedPtr ObjectPointer;
    public uint Flags;
}

public class FText : IEquatable<FText>, IDisposable
{
    internal FTextData Data;
    public bool IsValid => Data.ObjectPointer.Valid;
    
    public static readonly FText None = new();
    
    public FText()
    {
        FTextExporter.CallCreateEmptyText(ref Data);
    }
    
    public FText(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            FTextExporter.CallCreateEmptyText(ref Data);
            return;
        }

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
        if (text.IsEmpty)
        {
            FTextExporter.CallCreateEmptyText(ref Data);
            return;
        }

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
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (IsValid)
        {
            Data.ObjectPointer.Release();
            Data = default;
        }
    }
    
    /// <summary>
    /// Formats a localized text using ordered ({0}) placeholders.
    /// </summary>
    /// <param name="format">The format pattern "{0} {1}x".</param>
    /// <param name="args">The <see cref="FText"/> arguments to insert into the placeholders.</param>
    /// <returns>A new formatted <see cref="FText"/>.</returns>
    /// <example>
    /// <code>
    /// FText message = FText.Format("{0} {1}x", itemName, itemCount);
    /// </code>
    /// </example>
    public static FText Format(FText format, params FText[] args)
    {
        return UCSTextExtensions.Format(format, args);
    }

    /// <inheritdoc cref="Format(FText, FText[])" />
    public static FText Format(string format, params FText[] args)
    {
        return UCSTextExtensions.Format((FText)format, args);
    }
    
    public bool IsCultureInvariant => FTextExporter.CallIsCultureInvariant(ref Data).ToManagedBool();
    public bool IsEmpty => FTextExporter.CallIsEmpty(ref Data).ToManagedBool();
    public bool IsFromStringTable => FTextExporter.CallIsFromStringTable(ref Data).ToManagedBool();
    public bool IsNumeric => FTextExporter.CallIsNumeric(ref Data).ToManagedBool();
    public bool IsInitializedFromString => FTextExporter.CallIsInitializedFromString(ref Data).ToManagedBool();

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is FText other && Equals(other);
    }

    public bool Equals(FText? other)
    {
        if (other is null)
        {
            return false;
        }
        
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        
        return Data.ObjectPointer == other.Data.ObjectPointer;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Data.ObjectPointer.GetHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (!IsValid)
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
        if (!IsValid)
        {
            return ReadOnlySpan<char>.Empty;
        }

        unsafe
        {
            FTextExporter.CallToStringView(ref Data, out char* textPtr, out int length);
            return new ReadOnlySpan<char>(textPtr, length);
        }
    }
    
    public static implicit operator FText(string? value) => new(value);
    public static implicit operator string(FText value) => value.ToString();
    public static implicit operator ReadOnlySpan<char>(FText value) => value.AsReadOnlySpan();
    
    public static bool operator ==(FText? a, FText? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }
        
        if (a is null || b is null)
        {
            return false;
        }
        
        return a.Equals(b);
    }

    public static bool operator !=(FText? a, FText? b) => !(a == b);
}

public static class TextMarshaller
{ 
    public static void ToNative(IntPtr nativeBuffer, int arrayIndex, FText obj)
    {
        unsafe
        {
            FTextData* to = (FTextData*)(nativeBuffer + arrayIndex * sizeof(FTextData));
            to->ObjectPointer.Release();
            
            if (obj != null)
            {
                *to = obj.Data;
                to->ObjectPointer.AddRef();
            }
            else
            {
                *to = default;
            }
        }
    }

    public static FText FromNative(IntPtr nativeBuffer, int arrayIndex)
    {
        FTextData data = BlittableMarshaller<FTextData>.FromNative(nativeBuffer, arrayIndex);
        return new FText(data);
    }
}