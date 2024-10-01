using System.Runtime.InteropServices;

using UnrealSharp.Attributes;
using UnrealSharp.Interop;

namespace UnrealSharp;

[UStruct, StructLayout(LayoutKind.Sequential), BlittableType]
public struct FName : IEquatable<FName>, IComparable<FName>
{
    private int ComparisonIndex;
    private int DisplayIndex;
    private int Number;
    
    public static readonly FName None = new(0, 0);
    
    public FName(string name)
    {
        unsafe
        {
            fixed (char* stringPtr = name)
            {
                FNameExporter.CallStringToName(ref this, (IntPtr) stringPtr);
            }
        }
    }

    private FName(int comparisonIndex, int number)
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
    public bool IsValid()
    {
        return FNameExporter.CallIsValid(this);
    }
    
    /// <summary>
    /// Check if the name is None.
    /// </summary>
    /// <returns>True if the name is None, false otherwise.</returns>
    public bool IsNone()
    {
        return this == None;
    }
    
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
        return name.IsValid() ? name.ToString() : None.ToString();
    }
    
    public static implicit operator FText(FName name)
    {
        return name.IsValid() ? new FText(name) : FText.None;
    }
    
    public bool Equals(FName other)
    {
        return this == other;
    }
    
    public override bool Equals(object obj)
    {
        if (obj is FName name)
        {
            return this == name;
        }

        return false;
    }
    
    public override int GetHashCode()
    {
        return ComparisonIndex;
    }
    
    /// <summary>
    /// Compare two names.
    /// </summary>
    /// <param name="other">The name to compare against.</param>
    /// <returns>0 if the names are equal, a negative value if this name is less than the other name, and a positive value if this name is greater than the other name.</returns>
    public int CompareTo(FName other)
    {
        int diff = ComparisonIndex - other.ComparisonIndex;
        
        if (diff != 0)
        {
            return diff;
        }
        
        return Number - other.Number;
    }
}