using UnrealSharp.CoreUObject;

namespace UnrealSharp.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
class PropertyFlagsMapAttribute(NativePropertyFlags flags = NativePropertyFlags.None) : Attribute
{
    public NativePropertyFlags Flags = flags;
}

[Flags]
public enum PropertyFlags : ulong
{
    [PropertyFlagsMap]
    None = 0,
    
    [PropertyFlagsMap(NativePropertyFlags.Config)]
    Config = NativePropertyFlags.Config,
    
    [PropertyFlagsMap(NativePropertyFlags.ExportObject)]
    Export = NativePropertyFlags.ExportObject,
    
    [PropertyFlagsMap(NativePropertyFlags.NoClear)]
    NoClear = NativePropertyFlags.NoClear,
    
    [PropertyFlagsMap(NativePropertyFlags.EditFixedSize)]
    EditFixedSize = NativePropertyFlags.EditFixedSize,
    
    [PropertyFlagsMap(NativePropertyFlags.SaveGame)]
    SaveGame = NativePropertyFlags.SaveGame,
    
    [PropertyFlagsMap(NativePropertyFlags.BlueprintReadOnly)]
    BlueprintReadOnly = NativePropertyFlags.BlueprintReadOnly | NativePropertyFlags.BlueprintVisible,
    
    [PropertyFlagsMap(NativePropertyFlags.Edit)]
    BlueprintReadWrite = NativePropertyFlags.Edit | NativePropertyFlags.BlueprintVisible,
    
    [PropertyFlagsMap(NativePropertyFlags.Net)]
    Replicated = NativePropertyFlags.Net,
    
    [PropertyFlagsMap(NativePropertyFlags.EditDefaultsOnly)]
    EditDefaultsOnly = NativePropertyFlags.EditDefaultsOnly,
    
    [PropertyFlagsMap(NativePropertyFlags.EditInstanceOnly)]
    EditInstanceOnly = NativePropertyFlags.EditInstanceOnly,
    
    [PropertyFlagsMap(NativePropertyFlags.EditAnywhere)]
    EditAnywhere = NativePropertyFlags.EditAnywhere,
    
    [PropertyFlagsMap(NativePropertyFlags.BlueprintAssignable)]
    BlueprintAssignable = NativePropertyFlags.BlueprintAssignable | BlueprintReadOnly,
    
    [PropertyFlagsMap(NativePropertyFlags.BlueprintCallable)]
    BlueprintCallable = NativePropertyFlags.BlueprintCallable
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
[PropertyFlagsMap()]
public sealed class UPropertyAttribute : Attribute
{
    public UPropertyAttribute(PropertyFlags flags = PropertyFlags.None)
    {
        Flags = flags;
        ArrayDim = 1;
    }

    public PropertyFlags Flags
    {
        get;
        private set;
    }

    public bool DefaultComponent = false;
    public bool RootComponent = false;
    public string AttachmentComponent = "";
    public string AttachmentSocket = "";

    public string ReplicatedUsing = "";
    public LifetimeCondition LifetimeCondition = LifetimeCondition.None;

    public string BlueprintSetter = "";
    public string BlueprintGetter = "";
    
    public int ArrayDim;
}