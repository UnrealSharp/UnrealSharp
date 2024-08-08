using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UnrealSharpWeaver.MetaData;

public class BaseMetaData
{
    public string Name { get; set; }
    public Dictionary<string, string> MetaData { get; set; }
    
    // Non-serialized for JSON
    public readonly string AttributeName;
    public readonly IMemberDefinition MemberDefinition;
    public readonly CustomAttribute? BaseAttribute;
    // End non-serialized
    
    public BaseMetaData(MemberReference member, string attributeName)
    {
        MemberDefinition = member.Resolve();
        Name = WeaverHelper.GetEngineName(MemberDefinition);
        
        AttributeName = attributeName;
        MetaData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        BaseAttribute = WeaverHelper.FindAttribute(MemberDefinition.CustomAttributes, AttributeName);
        
        AddMetaData();
        AddBaseAttributes();
        AddDefaultCategory();
    }
    
    public void TryAddMetaData(string key, string value = "")
    {
        if (MetaData.TryAdd(key, value))
        {
            return;
        }
        
        MetaData[key] = value;
    }
    
    public void TryAddMetaData(string key, bool value)
    {
        TryAddMetaData(key, value ? "true" : "false");
    }
    
    public void TryAddMetaData(string key, int value)
    {
        TryAddMetaData(key, value.ToString());
    }
    
    public void TryAddMetaData(string key, ulong value)
    {
        TryAddMetaData(key, value.ToString());
    }
    
    public void TryAddMetaData(string key, float value)
    {
        TryAddMetaData(key, value.ToString());
    }
    
    public void TryAddMetaData(string key, double value)
    {
        TryAddMetaData(key, value.ToString());
    }
    
    public void AddDefaultCategory()
    {
        if (!MetaData.ContainsKey("Category"))
        {
            TryAddMetaData("Category", "Default");
        }
    }

    public static ulong GetFlags(IEnumerable<CustomAttribute> customAttributes, string flagsAttributeName)
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
            TypeDefinition? attributeClass = attribute.AttributeType.Resolve();
            
            if (attributeClass == null)
            {
                continue;
            }
            
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
        CustomAttribute?[] metaDataAttributes = WeaverHelper.FindMetaDataAttributes(MemberDefinition.CustomAttributes);
        foreach (var attrib in metaDataAttributes)
        {
            switch (attrib.ConstructorArguments.Count)
            {
                case < 1:
                    continue;
                case 1:
                    TryAddMetaData((string)attrib.ConstructorArguments[0].Value);
                    break;
                default:
                    TryAddMetaData((string)attrib.ConstructorArguments[0].Value, (string)attrib.ConstructorArguments[1].Value);
                    break;
            }
        }
    }

    private void AddBaseAttributes()
    {
        if (BaseAttribute == null)
        {
            return;
        }
        
        CustomAttributeArgument? displayNameArgument = WeaverHelper.FindAttributeField(BaseAttribute, "DisplayName");
        if (displayNameArgument.HasValue)
        {
            TryAddMetaData("DisplayName", (string) displayNameArgument.Value.Value);
        }

        CustomAttributeArgument? categoryArgument = WeaverHelper.FindAttributeField(BaseAttribute, "Category");
        if (categoryArgument.HasValue)
        {
            TryAddMetaData("Category", (string) categoryArgument.Value.Value);
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