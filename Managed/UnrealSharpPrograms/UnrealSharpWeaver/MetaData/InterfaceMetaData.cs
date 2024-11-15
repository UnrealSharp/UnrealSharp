using Mono.Cecil;

namespace UnrealSharpWeaver.MetaData;

public class InterfaceMetaData : TypeReferenceMetadata
{ 
    public List<FunctionMetaData> Functions { get; set; }
    
    // Non-serialized for JSON
    const string CannotImplementInterfaceInBlueprint = "CannotImplementInterfaceInBlueprint";
    // End non-serialized
    
    public InterfaceMetaData(TypeDefinition typeDefinition) : base(typeDefinition, WeaverHelper.UInterfaceAttribute)
    {
        Functions = [];
        
        foreach (var method in typeDefinition.Methods)
        {
            if (method.IsAbstract && WeaverHelper.IsUFunction(method))
            {
                Functions.Add(new FunctionMetaData(method, onlyCollectMetaData: true));
            }
        }
        
        CustomAttributeArgument? nonBpInterface = WeaverHelper.FindAttributeField(BaseAttribute, CannotImplementInterfaceInBlueprint);
        if (nonBpInterface != null)
        {
            TryAddMetaData(CannotImplementInterfaceInBlueprint, (bool) nonBpInterface.Value.Value);
        }
    }
}
