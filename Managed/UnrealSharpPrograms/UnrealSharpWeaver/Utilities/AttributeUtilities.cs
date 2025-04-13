using Mono.Cecil;

namespace UnrealSharpWeaver.Utilities;

public static class AttributeUtilities
{
    public static readonly string UMetaDataAttribute = "UMetaDataAttribute";
    public static readonly string MetaTagsNamespace = WeaverImporter.AttributeNamespace + ".MetaTags";
    
    public static List<CustomAttribute> FindMetaDataAttributes(this IEnumerable<CustomAttribute> customAttributes)
    {
        return FindAttributesByType(customAttributes, WeaverImporter.AttributeNamespace, UMetaDataAttribute);
    }

    public static List<CustomAttribute> FindMetaDataAttributesByNamespace(this IEnumerable<CustomAttribute> customAttributes)
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
        List<CustomAttribute> attribs = FindAttributesByType(customAttributes, typeNamespace, typeName);
        return attribs.Count == 0 ? null : attribs[0];
    }

    public static List<CustomAttribute> FindAttributesByType(this IEnumerable<CustomAttribute> customAttributes, string typeNamespace, string typeName)
    {
        List<CustomAttribute> attribs = new List<CustomAttribute>();
        foreach (CustomAttribute attrib in customAttributes)
        {
            if (attrib.AttributeType.Namespace == typeNamespace && attrib.AttributeType.Name == typeName)
            {
                attribs.Add(attrib);
            }
        }
        return attribs;
    }

    public static List<CustomAttribute> FindAttributesByNamespace(this IEnumerable<CustomAttribute> customAttributes, string typeNamespace)
    {
        List<CustomAttribute> attribs = new List<CustomAttribute>();
        foreach (var attrib in customAttributes)
        {
            if (attrib.AttributeType.Namespace == typeNamespace)
            {
                attribs.Add(attrib);
            }
        }
        return attribs;
    }
}