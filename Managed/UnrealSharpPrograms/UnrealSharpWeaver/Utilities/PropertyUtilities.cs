using Mono.Cecil;
using Mono.Collections.Generic;

namespace UnrealSharpWeaver.Utilities;

public static class PropertyUtilities
{
    public static readonly string UPropertyAttribute = "UPropertyAttribute";
    
    public static CustomAttribute? GetUProperty(Collection<CustomAttribute> attributes)
    {
        return attributes.FindAttributeByType(WeaverImporter.UnrealSharpAttributesNamespace, UPropertyAttribute);
    }
    
    public static CustomAttribute? GetUProperty(this IMemberDefinition typeDefinition)
    {
        return typeDefinition.CustomAttributes.FindAttributeByType(WeaverImporter.UnrealSharpAttributesNamespace, UPropertyAttribute);
    }
    
    public static bool IsUProperty(this IMemberDefinition property)
    {
        return GetUProperty(property.CustomAttributes) != null;
    }
}