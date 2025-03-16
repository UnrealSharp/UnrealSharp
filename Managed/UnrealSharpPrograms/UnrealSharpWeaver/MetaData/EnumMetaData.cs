using Mono.Cecil;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.MetaData;

public class EnumMetaData : TypeReferenceMetadata
{
    public List<string> Items { get; set; }

    public EnumMetaData(TypeDefinition enumType) : base(enumType, TypeDefinitionUtilities.UEnumAttribute)
    {
        Items = new List<string>();
        
        foreach (var field in enumType.Fields)
        {
            if (!field.IsStatic && field.Name == "value__")
            {
                continue;
            }
            
            Items.Add(field.Name);
        }
    }
}