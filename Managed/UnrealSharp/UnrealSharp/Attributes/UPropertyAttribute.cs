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
    
    [PropertyFlagsMap(NativePropertyFlags.InstancedReference)]
    Instanced = NativePropertyFlags.PersistentInstance,
    
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
    
    [PropertyFlagsMap(NativePropertyFlags.BlueprintReadWrite)]
    BlueprintReadWrite = NativePropertyFlags.BlueprintReadWrite,
    
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
    BlueprintCallable = NativePropertyFlags.BlueprintCallable,
    
    [PropertyFlagsMap(NativePropertyFlags.VisibleAnywhere)]
    VisibleAnywhere = NativePropertyFlags.VisibleAnywhere,
    
    [PropertyFlagsMap(NativePropertyFlags.VisibleDefaultsOnly)]
    VisibleDefaultsOnly = NativePropertyFlags.VisibleDefaultsOnly,
    
    [PropertyFlagsMap(NativePropertyFlags.VisibleInstanceOnly)]
    VisibleInstanceOnly = NativePropertyFlags.VisibleInstanceOnly,
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
[PropertyFlagsMap]
public sealed class UPropertyAttribute(PropertyFlags flags = PropertyFlags.None) : BaseUAttribute
{
    /// <summary>
    /// These flags determine how the property is handled in the engine.
    /// </summary>
    public PropertyFlags Flags = flags;

    /// <summary>
    /// Indicates whether this property is a component that should be automatically initialized as a default subobject of the Actor.
    /// Works only on properties of type ActorComponent.
    /// </summary>
    public bool DefaultComponent = false;

    /// <summary>
    /// Specifies whether this component is the root component of the Actor. If multiple components are marked as root, only the first
    /// one encountered in the order of declaration will be used as the root.
    /// </summary>
    public bool RootComponent = false;

    /// <summary>
    /// The name of another component to which this component should be attached. Specify the variable name of the
    /// target component as a string.
    /// </summary>
    public string AttachmentComponent = "";

    /// <summary>
    /// The name of the socket on the parent component to which this component should be attached.
    /// </summary>
    public string AttachmentSocket = "";
    
    /// <summary>
    ///   The callback function used when the property value changes on the server. Declaring this function
    ///   automatically enables replication for this property, so it's not necessary to explicitly mark it as replicated.
    ///   To use this, assign the name of the callback function to 'ReplicatedUsing'. For example:
    ///   ReplicatedUsing = "OnRep_PropertyName";
    ///   where "OnRep_PropertyName" is the method that will be called whenever the property is updated on clients.
    ///
    ///   Acceptable method signatures:
    ///     void OnRep_PropertyName() {}
    ///     void OnRep_PropertyName(int oldValue) {}
    /// </summary>
    public string ReplicatedUsing = "";
    
    /// <summary>
    /// The condition for the lifetime of the property.
    /// </summary>
    public LifetimeCondition LifetimeCondition = LifetimeCondition.None;

    /// <summary>
    /// The function to call when the property is changed.
    /// </summary>
    public string BlueprintSetter = "";
    
    /// <summary>
    /// The function to call when the property is getting accessed.
    /// </summary>
    public string BlueprintGetter = "";
    
    public int ArrayDim = 1;
}