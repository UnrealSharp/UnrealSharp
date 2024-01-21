using Mono.Cecil;

namespace UnrealSharpWeaver.MetaData;

public class InterfaceMetaData(TypeReference typeReference) : TypeReferenceMetadata(typeReference)
{ 
    public FunctionMetaData[] Functions { get; set; } = FunctionMetaData.PopulateFunctionArray(typeReference.Resolve());

    public static bool IsBlueprintInterface(TypeDefinition type)
    {
        if (!type.IsInterface)
        {
            return false;
        }
        
        foreach (var method in type.Methods)
        {
            if (FunctionMetaData.IsUFunction(method))
            {
                return true;
            }
        }

        return false;
    }
    
}