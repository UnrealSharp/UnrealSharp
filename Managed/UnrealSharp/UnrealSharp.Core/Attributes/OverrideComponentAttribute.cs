namespace UnrealSharp.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class OverrideComponentAttribute : Attribute
{
    public Type OverrideComponentType;
    public string OverridePropertyName;
    public string? OptionalPropertyName;
    
    /// <param name="overrideComponentType"> The type of the component to override with. For example, UMyCustomMeshComponent.</param>
    /// <param name="overridePropertyName"> The name of the property in the base class to override. For example, "MeshComponent".</param>
    /// <param name="optionalPropertyName"> An optional name for a wrapper property to expose the overridden component.</param>
    public OverrideComponentAttribute(Type overrideComponentType, string overridePropertyName, string? optionalPropertyName = null)
    {
        OverrideComponentType = overrideComponentType;
        OverridePropertyName = overridePropertyName;
        OptionalPropertyName = optionalPropertyName;
    }
}