namespace UnrealSharp.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
class ClassFlagsMapAttribute(NativeClassFlags flags = NativeClassFlags.None) : Attribute
{
    public NativeClassFlags Flags = flags;
}

[Flags]
public enum ClassFlags : ulong
{
    [ClassFlagsMap]
    None = 0x0,

    [ClassFlagsMap(NativeClassFlags.PerObjectConfig)]
    PerObjectConfig = 0x00000400u,
        
    [ClassFlagsMap(NativeClassFlags.DefaultConfig)]
    DefaultConfig = 0x00000002u,
        
    [ClassFlagsMap(NativeClassFlags.Config)]
    Config = 0x00000004u,

    [ClassFlagsMap(NativeClassFlags.DefaultToInstanced)]
    DefaultToInstanced = 0x00200000u,
    
    [ClassFlagsMap(NativeClassFlags.Abstract)]
    Abstract = 0x00000001u,
    
    [ClassFlagsMap(NativeClassFlags.Deprecated)]
    Deprecated = 0x02000000u,
    
    [ClassFlagsMap(NativeClassFlags.Transient)]
    Transient = 0x00000008u,
}

[AttributeUsage(AttributeTargets.Class)]
[ClassFlagsMap]
public sealed class UClassAttribute(ClassFlags Flags = ClassFlags.None) : Attribute
{
    public ClassFlags Flags { get; private set; } = Flags;
    public string ConfigCategory { get; set; }
}