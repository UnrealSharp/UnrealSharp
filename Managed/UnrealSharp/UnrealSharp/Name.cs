using System.Runtime.InteropServices;

using UnrealSharp.Attributes;
using UnrealSharp.Interop;

namespace UnrealSharp;

[UStruct(IsBlittable=true), StructLayout(LayoutKind.Sequential)]
public struct Name : IEquatable<Name>, IComparable<Name>
{
    private int ComparisonIndex;
    private int DisplayIndex;
    private int Number;
    
    public static readonly Name None = new(0, 0);
    
    public Name(string name)
    {
        unsafe
        {
            fixed (char* stringPtr = name)
            {
                FNameExporter.CallStringToName(ref this, (IntPtr) stringPtr);
            }
        }
    }

    private Name(int comparisonIndex, int number)
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
                FStringExporter.CallDisposeString(ref buffer);
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
    
    public static bool operator == (Name lhs, Name rhs)
    {
        return lhs.ComparisonIndex == rhs.ComparisonIndex && lhs.Number == rhs.Number;
    }
    
    public static bool operator != (Name lhs, Name rhs)
    {
        return !(lhs == rhs);
    }
    
    public static implicit operator Name(string name)
    {
        return name.Length != 0 ? new Name(name) : None;
    }
    
    public static implicit operator string(Name name)
    {
        return name.IsValid() ? name.ToString() : None.ToString();
    }
    
    public static implicit operator Text(Name name)
    {
        return name.IsValid() ? new Text(name) : Text.None;
    }
    
    public bool Equals(Name other)
    {
        return this == other;
    }
    
    public override bool Equals(object obj)
    {
        if (obj is Name name)
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
    public int CompareTo(Name other)
    {
        int diff = ComparisonIndex - other.ComparisonIndex;
        
        if (diff != 0)
        {
            return diff;
        }
        
        return Number - other.Number;
    }
}