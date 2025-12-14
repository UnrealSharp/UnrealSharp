using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public readonly record struct ComponentOverride 
{
    public readonly FieldName OwningClass;
    
    public readonly FieldName OverrideComponentType;
    public readonly string OverridePropertyName;
    public readonly Accessibility Accessibility;
    public readonly string? OptionalPropertyName;
    
    public ComponentOverride(ITypeSymbol owningClass, ITypeSymbol overrideComponentType, string overridePropertyName, Accessibility accessibility, string? optionalPropertyName = null)
    {
        OwningClass = new FieldName(owningClass);
        OverrideComponentType = new FieldName(overrideComponentType);
        OverridePropertyName = overridePropertyName;
        Accessibility = accessibility;
        OptionalPropertyName = optionalPropertyName;
    }
}