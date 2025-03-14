using Mono.Cecil;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.MetaData;

public class InterfaceMetaData : TypeReferenceMetadata
{ 
    public List<FunctionMetaData> Functions { get; set; }
    
    // Non-serialized for JSON
    const string CannotImplementInterfaceInBlueprint = "CannotImplementInterfaceInBlueprint";
    // End non-serialized
    
    public InterfaceMetaData(TypeDefinition typeDefinition) : base(typeDefinition, TypeDefinitionUtilities.UInterfaceAttribute)
    {
        Functions = [];
        
        foreach (var method in typeDefinition.Methods)
        {
            if (method.IsAbstract && method.IsUFunction())
            {
                Functions.Add(new FunctionMetaData(method, onlyCollectMetaData: true));
            }
        }
        
        CustomAttributeArgument? nonBpInterface = BaseAttribute.FindAttributeField(CannotImplementInterfaceInBlueprint);
        if (nonBpInterface != null)
        {
            TryAddMetaData(CannotImplementInterfaceInBlueprint, (bool) nonBpInterface.Value.Value);
        }
    }
}
