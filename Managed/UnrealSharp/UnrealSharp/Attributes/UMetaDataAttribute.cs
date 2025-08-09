namespace UnrealSharp.Attributes;

/// <summary>
/// [UMetaData("key", "value")]
/// Directly set the key and value for your MetaData
/// Note: There are specific MetaTags available (e.g. [HideSelfPin]) to avoid setting via magic strings
/// but this allows full control to add any new or missing key
/// https://dev.epicgames.com/documentation/en-us/unreal-engine/metadata-specifiers-in-unreal-engine
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple=true)]
public sealed class UMetaDataAttribute : Attribute
{       
    public UMetaDataAttribute(string key, string value = "")
    {
    }
}
