using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.TypeProcessors;

public static class UnrealEnumProcessor
{ 
    public static void ProcessEnums(List<TypeDefinition> foundEnums, ApiMetaData assemblyMetadata)
    {
        assemblyMetadata.EnumMetaData = new EnumMetaData[foundEnums.Count];
        for (var i = 0; i < foundEnums.Count; i++)
        {
            assemblyMetadata.EnumMetaData[i] = new EnumMetaData(foundEnums[i]);
        }
    }
}