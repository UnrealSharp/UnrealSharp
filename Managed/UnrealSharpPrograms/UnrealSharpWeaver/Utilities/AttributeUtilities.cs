using Mono.Cecil;

namespace UnrealSharpWeaver.Utilities;

public static class AttributeUtilities
{
    public static readonly string UMetaDataAttribute = "UMetaDataAttribute";
    public static readonly string MetaTagsNamespace = WeaverImporter.AttributeNamespace + ".MetaTags";
    
    public static CustomAttribute?[] FindMetaDataAttributes(this IEnumerable<CustomAttribute> customAttributes)
    {
        return FindAttributesByType(customAttributes, WeaverImporter.AttributeNamespace, UMetaDataAttribute);
    }

    public static CustomAttribute[] FindMetaDataAttributesByNamespace(this IEnumerable<CustomAttribute> customAttributes)
    {
        return FindAttributesByNamespace(customAttributes, MetaTagsNamespace);
    }

    public static CustomAttributeArgument? FindAttributeField(this CustomAttribute attribute, string fieldName)
    {
        foreach (var field in attribute.Fields) 
        {
            if (field.Name == fieldName) 
            {
                return field.Argument;
            }
        }
        return null;
    }
    
    public static CustomAttribute? FindAttributeByType(this IEnumerable<CustomAttribute> customAttributes, string typeNamespace, string typeName)
    {
        CustomAttribute?[] attribs = FindAttributesByType(customAttributes, typeNamespace, typeName);
        return attribs.Length == 0 ? null : attribs[0];
    }

    public static CustomAttribute?[] FindAttributesByType(this IEnumerable<CustomAttribute> customAttributes, string typeNamespace, string typeName)
    {
        return (from attrib in customAttributes
            where attrib.AttributeType.Namespace == typeNamespace && attrib.AttributeType.Name == typeName
            select attrib).ToArray ();
    }

    public static CustomAttribute[] FindAttributesByNamespace(this IEnumerable<CustomAttribute> customAttributes, string typeNamespace)
    {
        return (from attrib in customAttributes
            where attrib.AttributeType.Namespace == typeNamespace
            select attrib).ToArray();
    }
}