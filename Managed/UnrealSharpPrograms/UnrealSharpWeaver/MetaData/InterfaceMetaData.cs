using Mono.Cecil;

namespace UnrealSharpWeaver.MetaData;

public class InterfaceMetaData : TypeReferenceMetadata
{ 
    public List<FunctionMetaData> Functions { get; set; }
    
    public InterfaceMetaData(TypeDefinition typeDefinition) : base(typeDefinition, WeaverHelper.UInterfaceAttribute)
    {
        CustomAttribute? interfaceAttributes = WeaverHelper.GetUInterface(typeDefinition);
        CustomAttributeArgument? nonBpInterface = WeaverHelper.FindAttributeField(interfaceAttributes, "CannotImplementInterfaceInBlueprint");
        
        if (nonBpInterface != null)
        {
            var cannotImplementInterfaceInBlueprint = (bool) nonBpInterface.Value.Value;
            
            // Only add the metadata if it's true, since it's false by default
            if (cannotImplementInterfaceInBlueprint)
            {
                MetaData.Add("CannotImplementInterfaceInBlueprint", cannotImplementInterfaceInBlueprint.ToString());
            }
        }
        
        Functions = [];
        foreach (var method in typeDefinition.Methods)
        {
            if (method.IsAbstract && WeaverHelper.IsUFunction(method))
            {
                Functions.Add(new FunctionMetaData(method));
            }
        }
    }
}
