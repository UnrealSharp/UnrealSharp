using System;
using System.Collections.Generic;
using System.Linq;

namespace UnrealSharpScriptGenerator.Model;

public record struct SpecialStructInfo
{
    public required Dictionary<string, BlittableStructInfo> BlittableTypes { get; init; }
    
    public required Dictionary<string, NativelyTranslatableStructInfo> NativelyCopyableTypes { get; init; }

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
        BlittableTypes = [],
        NativelyCopyableTypes = []
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