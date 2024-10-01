namespace UnrealSharp.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple=true)]
public sealed class UMetaDataAttribute : Attribute
{       
    public UMetaDataAttribute(string key, string value = "")
    {
    }
}