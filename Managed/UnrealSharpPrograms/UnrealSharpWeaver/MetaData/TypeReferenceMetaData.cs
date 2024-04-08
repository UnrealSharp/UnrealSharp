using Mono.Cecil;

namespace UnrealSharpWeaver.MetaData;

public class TypeReferenceMetadata : BaseMetaData
{
    public TypeReference TypeDef;
    
    public TypeReferenceMetadata(TypeDefinition typeDef, string attributeName = "")
    {
        AssemblyName = typeDef.Module.Assembly.Name.Name;
        Namespace = typeDef.Namespace;
        Name = typeDef.Name;
        TypeDef = typeDef;
        
        AddMetadataAttributes(typeDef.CustomAttributes);
        
        if (attributeName == "")
        {
            return;
        }
        
        CustomAttribute? baseAttribute = FindAttribute(typeDef.CustomAttributes, attributeName);
        
        if (baseAttribute == null)
        {
            return;
        }
        
        var categoryField = WeaverHelper.FindAttributeField(baseAttribute, "Category");
        
        if (categoryField != null)
        {
            MetaData.Add("Category", (string) categoryField.Value.Value);
        }
        
        var displayNameField = WeaverHelper.FindAttributeField(baseAttribute, "DisplayName");
        
        if (displayNameField != null)
        {
            MetaData.Add("DisplayName", (string) displayNameField.Value.Value);
        }
    }
}