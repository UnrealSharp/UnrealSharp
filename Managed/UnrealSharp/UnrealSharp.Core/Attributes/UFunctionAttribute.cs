namespace UnrealSharp.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Property)]
class FunctionFlagsMapAttribute(NativeFunctionFlags flags = NativeFunctionFlags.None) : Attribute
{
    public NativeFunctionFlags Flags = flags;
}

[Flags]
public enum FunctionFlags : ulong
{
    [FunctionFlagsMap]
    None = 0x00000000,

    [FunctionFlagsMap(NativeFunctionFlags.BlueprintCallable)]
    BlueprintCallable = NativeFunctionFlags.BlueprintCallable,

    [FunctionFlagsMap(NativeFunctionFlags.BlueprintAuthorityOnly)]
    BlueprintAuthorityOnly = NativeFunctionFlags.BlueprintAuthorityOnly,

    [FunctionFlagsMap(NativeFunctionFlags.BlueprintCosmetic)]
    BlueprintCosmetic = NativeFunctionFlags.BlueprintCosmetic,

    [FunctionFlagsMap(NativeFunctionFlags.BlueprintPure)]
    BlueprintPure = NativeFunctionFlags.BlueprintPure,

    [FunctionFlagsMap(NativeFunctionFlags.BlueprintNativeEvent)]
    BlueprintEvent = NativeFunctionFlags.BlueprintNativeEvent,

    [FunctionFlagsMap(NativeFunctionFlags.NetMulticast)]
    Multicast = NativeFunctionFlags.NetMulticast,

    [FunctionFlagsMap(NativeFunctionFlags.NetServer)]
    RunOnServer = NativeFunctionFlags.NetServer,

    [FunctionFlagsMap(NativeFunctionFlags.NetClient)]
    RunOnClient = NativeFunctionFlags.NetClient,
    
    [FunctionFlagsMap(NativeFunctionFlags.NetReliable)]
    Reliable = NativeFunctionFlags.NetReliable,
    
    [FunctionFlagsMap(NativeFunctionFlags.Exec)]
    Exec = NativeFunctionFlags.Exec,
}

[AttributeUsage(AttributeTargets.Method)]
[FunctionFlagsMap(NativeFunctionFlags.Native)] 
public sealed class UFunctionAttribute(FunctionFlags flags = FunctionFlags.None) : BaseUAttribute
{
    /// <summary>
    /// The flags of the function.
    /// </summary>
    public FunctionFlags Flags = flags;

    /// <summary>
    /// If true, the function can be called from an instance of the class in the editor.
    /// </summary>
    public bool CallInEditor = false;
}