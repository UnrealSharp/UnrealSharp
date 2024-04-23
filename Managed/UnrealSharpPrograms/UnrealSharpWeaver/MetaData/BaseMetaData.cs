using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace UnrealSharpWeaver.MetaData;

public class BaseMetaData
{
    public string Name { get; set; }
    public Dictionary<string, string> MetaData { get; set; }
    
    // Non-serialized for JSON
    public readonly string AttributeName;
    public readonly IMemberDefinition MemberDefinition;
    // End non-serialized
    
    public BaseMetaData(MemberReference member, string attributeName)
    {
        Name = member.Name;
        AttributeName = attributeName;
        MetaData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        MemberDefinition = member.Resolve();
        
        AddMetaData();
        AddBaseAttributes();
    }

    public static ulong GetFlags(Collection<CustomAttribute> customAttributes, string flagsAttributeName)
    {
        CustomAttribute? flagsAttribute = WeaverHelper.FindAttributeByType(customAttributes, WeaverHelper.UnrealSharpNamespace + ".Attributes", flagsAttributeName);
        return flagsAttribute == null ? 0 : GetFlags(flagsAttribute);
    }

    public static ulong GetFlags(CustomAttribute flagsAttribute)
    {
        return (ulong) flagsAttribute.ConstructorArguments[0].Value;
    }

    public static ulong ExtractBoolAsFlags(TypeDefinition attributeType, CustomAttributeNamedArgument namedArg, string flagsAttributeName)
    {
        var arg = namedArg.Argument;
        
        if (!(bool)arg.Value)
        {
            return 0;
        }
        
        // Find the property definition for this argument to resolve the true value to the desired flags map.
        var properties = (from prop in attributeType.Properties where prop.Name == namedArg.Name select prop).ToArray();
        TypeProcessors.ConstructorBuilder.VerifySingleResult(properties, attributeType, "attribute property " + namedArg.Name);
        return GetFlags(properties[0].CustomAttributes, flagsAttributeName);
    }

    public static ulong ExtractStringAsFlags(TypeDefinition attributeType, CustomAttributeNamedArgument namedArg, string flagsAttributeName)
    {
        var arg = namedArg.Argument;
        var argValue = (string) arg.Value;
        
        if (argValue is not { Length: > 0 })
        {
            return 0;
        }

        PropertyDefinition? foundProperty = WeaverHelper.FindPropertyByName(attributeType.Properties, namedArg.Name);

        if (foundProperty == null)
        {
            return 0;
        }
        
        TypeProcessors.ConstructorBuilder.VerifySingleResult([foundProperty], attributeType, "attribute property " + namedArg.Name);
        return GetFlags(foundProperty.CustomAttributes, flagsAttributeName);

    }

    public static ulong GetFlags(IMemberDefinition member, string flagsAttributeName)
    {
        SequencePoint sequencePoint = ErrorEmitter.GetSequencePointFromMemberDefinition(member);
        var customAttributes = member.CustomAttributes;
        ulong flags = 0;

        foreach (CustomAttribute attribute in customAttributes)
        {
            TypeDefinition attributeClass = attribute.AttributeType.Resolve();
            CustomAttribute? flagsMap = WeaverHelper.FindAttribute(attributeClass.CustomAttributes, flagsAttributeName);

            if (flagsMap == null)
            {
                continue;
            }
            
            flags |= GetFlags(flagsMap);

            if (attribute.HasConstructorArguments)
            {
                foreach (CustomAttributeArgument arg in attribute.ConstructorArguments)
                {
                    flags |= Convert.ToUInt64(arg.Value);
                }
            }

            if (!attribute.HasProperties)
            {
                continue;
            }
                
            foreach (CustomAttributeNamedArgument arg in attribute.Properties)
            {
                TypeDefinition argType = arg.Argument.Type.Resolve();
                    
                if (argType.IsValueType && argType.Namespace == "System" && argType.Name == "Boolean")
                {
                    flags |= ExtractBoolAsFlags(attributeClass, arg, flagsAttributeName);
                }
                else if (argType.Namespace == "System" && argType.Name == "String")
                {
                    flags |= ExtractStringAsFlags(attributeClass, arg, flagsAttributeName);
                }
                else
                {
                    throw new InvalidAttributeException(attributeClass, sequencePoint, $"{argType.FullName} is not supported as an attribute property type.");
                }
            }
        }

        return flags;
    }
    
    private void AddMetaData()
    {
        var metaDataAttributes = WeaverHelper.FindMetaDataAttributes(MemberDefinition.CustomAttributes);

        foreach (var attrib in metaDataAttributes)
        {
            switch (attrib.ConstructorArguments.Count)
            {
                case < 1:
                    continue;
                case 1:
                    MetaData.Add((string)attrib.ConstructorArguments[0].Value, "");
                    break;
                default:
                    MetaData.Add((string)attrib.ConstructorArguments[0].Value, (string) attrib.ConstructorArguments[1].Value);
                    break;
            }
        }
    }

    private void AddBaseAttributes()
    {
        if (MemberDefinition.CustomAttributes == null || string.IsNullOrEmpty(AttributeName))
        {
            return;
        }
        
        CustomAttribute? baseAttribute = WeaverHelper.FindAttribute(MemberDefinition.CustomAttributes, AttributeName);
        
        if (baseAttribute == null)
        {
            return;
        }
        
        CustomAttributeArgument? displayNameArgument = WeaverHelper.FindAttributeField(baseAttribute, "DisplayName");

        if (displayNameArgument.HasValue)
        {
            MetaData.Add("DisplayName", (string) displayNameArgument.Value.Value);
        }

        CustomAttributeArgument? categoryArgument = WeaverHelper.FindAttributeField(baseAttribute, "Category");

        if (categoryArgument.HasValue)
        {
            MetaData.Add("Category", (string) categoryArgument.Value.Value);
        }
    }
    
    protected bool GetBoolMetadata(string key)
    {
        if (!MetaData.TryGetValue(key, out var val))
        {
            return false;
        }
        
        return 0 == StringComparer.OrdinalIgnoreCase.Compare(val, "true");
    }
}