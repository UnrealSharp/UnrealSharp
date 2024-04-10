using Mono.Cecil;

namespace UnrealSharpWeaver.MetaData;

public class EnumMetaData : TypeReferenceMetadata
{
    public List<string> Items { get; set; }

    public EnumMetaData(TypeDefinition enumType) : base(enumType, "UEnumAttribute")
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