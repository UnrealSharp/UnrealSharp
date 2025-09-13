using System.Runtime.InteropServices;

using UnrealSharp.Attributes;
using UnrealSharp.Core;
using UnrealSharp.Interop;

namespace UnrealSharp;

[UStruct, StructLayout(LayoutKind.Sequential)]
public struct FName : IEquatable<FName>, IComparable<FName>
{
#if !WITH_EDITOR
    private uint ComparisonIndex;
    private uint Number;
#else
	private uint ComparisonIndex;
    private uint DisplayIndex;
    private uint Number;
#endif

    public static readonly FName None = new(0, 0);
    
    public FName(string name)
    {
        unsafe
        {
            fixed (char* stringPtr = name)
            {
                FNameExporter.CallStringToName(ref this, stringPtr, name.Length);
            }
        }
    }
    
    public FName(ReadOnlySpan<char> name)
    {
        unsafe
        {
            fixed (char* stringPtr = name)
            {
                FNameExporter.CallStringToName(ref this, stringPtr, name.Length);
            }
        }
    }

    private FName(uint comparisonIndex, uint number)
    {
        ComparisonIndex = comparisonIndex;
        Number = number;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        unsafe
        {
            UnmanagedArray buffer = new UnmanagedArray();
            try
            {
                FNameExporter.CallNameToString(this, ref buffer);
                return new string((char*)buffer.Data);
            }
            finally
            {
                buffer.Destroy();
            }
        }
    }
    
    /// <summary>
    /// Check if the name is valid.
    /// </summary>
    /// <returns>True if the name is valid, false otherwise.</returns>
    public bool IsValid => FNameExporter.CallIsValid(this).ToManagedBool();
    
    /// <summary>
    /// Check if the name is None.
    /// </summary>
    /// <returns>True if the name is None, false otherwise.</returns>
    public bool IsNone => this == None;
    
    public static bool operator == (FName lhs, FName rhs)
    {
        return lhs.ComparisonIndex == rhs.ComparisonIndex && lhs.Number == rhs.Number;
    }
    
    public static bool operator != (FName lhs, FName rhs)
    {
        return !(lhs == rhs);
    }
    
    public static implicit operator FName(string name)
    {
        return name.Length != 0 ? new FName(name) : None;
    }
    
    public static implicit operator string(FName name)
    {
        return name.IsValid ? name.ToString() : None.ToString();
    }
    
    public static implicit operator FText(FName name)
    {
        return name.IsValid ? new FText(name) : FText.None;
    }
    
    public bool Equals(FName other)
    {
        return this == other;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is FName name)
        {
            return this == name;
        }

        return false;
    }
    
    public override int GetHashCode()
    {
        return (int)ComparisonIndex;
    }
    
    /// <summary>
    /// Compare two names.
    /// </summary>
    /// <param name="other">The name to compare against.</param>
    /// <returns>0 if the names are equal, a negative value if this name is less than the other name, and a positive value if this name is greater than the other name.</returns>
    public int CompareTo(FName other)
    {
        uint diff = ComparisonIndex - other.ComparisonIndex;
        
        if (diff != 0)
        {
            return (int)diff;
        }
        
        return (int)(Number - other.Number);
    }
}
