using System.Collections.Generic;

namespace UnrealSharpScriptGenerator.Model;

public record struct SpecialStructInfo
{
    public required Dictionary<string, BlittableStructInfo> BlittableTypes { get; init; }
    
    public required Dictionary<string, NativelyTranslatableStructInfo> NativelyCopyableTypes { get; init; }
}

public record SpecialTypeInfo
{
    public SpecialStructInfo Structs { get; } = new()
    {
        BlittableTypes = [],
        NativelyCopyableTypes = []
    };
}