namespace UnrealSharp.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
class ClassFlagsMapAttribute(NativeClassFlags flags = NativeClassFlags.None) : Attribute
{
    public NativeClassFlags Flags = flags;
}

[Flags]
public enum ClassFlags : ulong
{
    [ClassFlagsMapAttribute(NativeClassFlags.None)]
    None				  = 0x00000000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.Abstract)]
    Abstract            = 0x00000001u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.DefaultConfig)]
    DefaultConfig		  = 0x00000002u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.Config)]
    Config			  = 0x00000004u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.Transient)]
    Transient			  = 0x00000008u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.Optional)]
    Optional            = 0x00000010u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.MatchedSerializers)]
    MatchedSerializers  = 0x00000020u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.ProjectUserConfig)]
    ProjectUserConfig	  = 0x00000040u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.Native)]
    Native			  = 0x00000080u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.NoExport)]
    NoExport = 0x00000100u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.NotPlaceable)]
    NotPlaceable        = 0x00000200u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.PerObjectConfig)]
    PerObjectConfig     = 0x00000400u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.ReplicationDataIsSetUp)]
    ReplicationDataIsSetUp = 0x00000800u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.EditInlineNew)]
    EditInlineNew		  = 0x00001000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.CollapseCategories)]
    CollapseCategories  = 0x00002000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.Interface)]
    Interface           = 0x00004000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.CustomConstructor)]
    CustomConstructor = 0x00008000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.Const)]
    Const			      = 0x00010000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.NeedsDeferredDependencyLoading)]
    NeedsDeferredDependencyLoading = 0x00020000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.CompiledFromBlueprint)]
    CompiledFromBlueprint  = 0x00040000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.MinimalAPI)]
    MinimalAPI	      = 0x00080000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.RequiredAPI)]
    RequiredAPI	      = 0x00100000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.DefaultToInstanced)]
    DefaultToInstanced  = 0x00200000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.TokenStreamAssembled)]
    TokenStreamAssembled  = 0x00400000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.HasInstancedReference)]
    HasInstancedReference= 0x00800000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.Hidden)]
    Hidden			  = 0x01000000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.Deprecated)]
    Deprecated		  = 0x02000000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.HideDropDown)]
    HideDropDown		  = 0x04000000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.GlobalUserConfig)]
    GlobalUserConfig	  = 0x08000000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.Intrinsic)]
    Intrinsic			  = 0x10000000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.Constructed)]
    Constructed		  = 0x20000000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.ConfigDoNotCheckDefaults)]
    ConfigDoNotCheckDefaults = 0x40000000u,
    
    [ClassFlagsMapAttribute(NativeClassFlags.NewerVersionExists)]
    NewerVersionExists  = 0x80000000u,
    
}

[AttributeUsage(AttributeTargets.Class)]
[ClassFlagsMap]
public sealed class UClassAttribute(ClassFlags flags = ClassFlags.None) : Attribute
{
    public ClassFlags Flags { get; private set; } = flags;
    public string ConfigCategory { get; set; }
}