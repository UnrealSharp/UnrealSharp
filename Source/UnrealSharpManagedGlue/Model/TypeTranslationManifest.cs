using System.Collections.Immutable;

namespace UnrealSharpManagedGlue.Model;

public record struct BlittableStructInfo(string Name, string? ManagedType = null);

public record struct NativelyTranslatableStructInfo(string Name, bool HasDestructor);

public struct StructTranslationInfo
{
    public StructTranslationInfo() { }

    public ImmutableArray<string> CustomTypes { get; init; } = new ();
    public ImmutableArray<BlittableStructInfo> BlittableTypes { get; init; } = new();
    public ImmutableArray<NativelyTranslatableStructInfo> NativelyTranslatableTypes { get; init; } = new ();
}

public record TypeTranslationManifest
{
    public StructTranslationInfo Structs { get; init; }
}