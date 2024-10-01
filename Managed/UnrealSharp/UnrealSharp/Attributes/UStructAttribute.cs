namespace UnrealSharp.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
class StructFlagsMapAttribute(StructFlags flags = StructFlags.NoFlags) : Attribute
{
    public StructFlags Flags = flags;
}

[AttributeUsage(AttributeTargets.Struct)]
[StructFlagsMap(StructFlags.Native)]
public sealed class UStructAttribute : BaseUAttribute;