using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.TypeProcessors;

public static class UnrealEnumProcessor
{ 
    public static void ProcessEnums(List<TypeDefinition> foundEnums, ApiMetaData assemblyMetadata)
    {
        assemblyMetadata.EnumMetaData.Capacity = foundEnums.Count;
        
        for (var i = 0; i < foundEnums.Count; i++)
        {
            assemblyMetadata.EnumMetaData.Add(new EnumMetaData(foundEnums[i]));
        }
    }
}