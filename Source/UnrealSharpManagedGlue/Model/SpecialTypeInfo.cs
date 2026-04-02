using System;
using System.Collections.Generic;

namespace UnrealSharpManagedGlue.Model;

public record struct SpecialStructInfo
{
    public HashSet<string> SkippedTypes { get; init; }
    
    public Dictionary<string, BlittableStructInfo> BlittableTypes { get; init; }
    
    public Dictionary<string, NativelyTranslatableStructInfo> NativelyCopyableTypes { get; init; }

    public bool Equals(SpecialStructInfo other)
    {
        if (BlittableTypes.Count != other.BlittableTypes.Count || NativelyCopyableTypes.Count != other.NativelyCopyableTypes.Count)
        {
            return false;
        }

        foreach (var (key, value) in BlittableTypes)
        {
            if (!other.BlittableTypes.TryGetValue(key, out var otherValue) || value != otherValue)
            {
                return false;
            }
        }
        
        foreach (var (key, value) in NativelyCopyableTypes)
        {
            if (!other.NativelyCopyableTypes.TryGetValue(key, out var otherValue) || value != otherValue)
            {
                return false;
            }
        }
        
        return true;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(BlittableTypes, NativelyCopyableTypes);
    }
}

public record SpecialTypeInfo
{
    public SpecialStructInfo Structs { get; init; } = new()
    {
        SkippedTypes = new (),
        BlittableTypes = new Dictionary<string, BlittableStructInfo>(),
        NativelyCopyableTypes = new Dictionary<string, NativelyTranslatableStructInfo>()
    };

    public virtual bool Equals(SpecialTypeInfo? other)
    {
        return other is not null && Structs.Equals(other.Structs);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Structs);
    }
}