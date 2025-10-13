using System.Collections.Immutable;

namespace UnrealSharpScriptGenerator.Model;

public record struct BlittableStructInfo(string Name, string? ManagedType = null);

public record struct NativelyTranslatableStructInfo(string Name, bool HasDestructor);

public struct StructTranslationInfo()
{
    public ImmutableArray<string> CustomTypes { get; init; } = [];
    public ImmutableArray<BlittableStructInfo> BlittableTypes { get; init; } = [];
    public ImmutableArray<NativelyTranslatableStructInfo> NativelyTranslatableTypes { get; init; } = [];
}

public record TypeTranslationManifest
{
    public StructTranslationInfo Structs { get; init; }
}