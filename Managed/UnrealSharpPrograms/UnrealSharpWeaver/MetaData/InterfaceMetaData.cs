using Mono.Cecil;

namespace UnrealSharpWeaver.MetaData;

public class InterfaceMetaData : TypeReferenceMetadata
{ 
    public List<FunctionMetaData> Functions { get; set; }
    
    public InterfaceMetaData(TypeDefinition typeDefinition) : base(typeDefinition, "UInterfaceAttribute")
    {
        AddMetadataAttributes(typeDefinition.CustomAttributes);
        
        CustomAttribute? interfaceAttributes = 
            FindAttribute(typeDefinition.CustomAttributes, "UInterfaceAttribute");
        
        CustomAttributeArgument? cannotImplementInterfaceInBlueprintField = 
            WeaverHelper.FindAttributeField(interfaceAttributes, "CannotImplementInterfaceInBlueprint");
        
        if (cannotImplementInterfaceInBlueprintField != null)
        {
            var cannotImplementInterfaceInBlueprint = (bool) cannotImplementInterfaceInBlueprintField.Value.Value;
            
            // Only add the metadata if it's true, since it's false by default
            if (cannotImplementInterfaceInBlueprint)
            {
                MetaData.Add("CannotImplementInterfaceInBlueprint", cannotImplementInterfaceInBlueprint.ToString());
            }
        }
        
        Functions = new List<FunctionMetaData>();
        
        foreach (var method in typeDefinition.Methods)
        {
            if (method.IsAbstract && FunctionMetaData.IsUFunction(method))
            {
                Functions.Add(new FunctionMetaData(method));
            }
        }
    }
}