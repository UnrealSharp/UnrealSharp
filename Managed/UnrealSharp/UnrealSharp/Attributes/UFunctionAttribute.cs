namespace UnrealSharp.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Property)]
class FunctionFlagsMapAttribute(NativeFunctionFlags flags = NativeFunctionFlags.None) : Attribute
{
    public NativeFunctionFlags Flags = flags;
}

[Flags]
public enum FunctionFlags
{
    [FunctionFlagsMap]
    None = 0x00000000,

    [FunctionFlagsMap(NativeFunctionFlags.BlueprintCallable)]
    BlueprintCallable = 0x04000000,

    [FunctionFlagsMap(NativeFunctionFlags.BlueprintAuthorityOnly)]
    BlueprintAuthorityOnly = 0x00000004,

    [FunctionFlagsMap(NativeFunctionFlags.BlueprintCosmetic)]
    BlueprintCosmetic = 0x00000008,

    [FunctionFlagsMap(NativeFunctionFlags.BlueprintPure)]
    BlueprintPure = 0x10000000,

    [FunctionFlagsMap(NativeFunctionFlags.BlueprintNativeEvent)]
    BlueprintEvent = 0x08000000,

    [FunctionFlagsMap(NativeFunctionFlags.NetMulticast)]
    Multicast = 0x00004000,

    [FunctionFlagsMap(NativeFunctionFlags.NetServer)]
    RunOnServer = 0x00200000,

    [FunctionFlagsMap(NativeFunctionFlags.NetClient)]
    RunOnClient = 0x01000000,
    
    [FunctionFlagsMap(NativeFunctionFlags.NetReliable)]
    Reliable = 0x00000080,
}

[AttributeUsage(AttributeTargets.Method)]
[FunctionFlagsMap(NativeFunctionFlags.Native)] 
public sealed class UFunctionAttribute(FunctionFlags flags = FunctionFlags.None) : Attribute
{
    public FunctionFlags Flags { get; private set; } = flags;
}