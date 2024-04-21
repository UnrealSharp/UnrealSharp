using Mono.Cecil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.TypeProcessors;

public static class UnrealInterfaceProcessor
{ 
    public static void ProcessInterfaces(List<TypeDefinition> interfaces, ApiMetaData assemblyMetadata)
    {
        assemblyMetadata.InterfacesMetaData = new InterfaceMetaData[interfaces.Count];
        
        for (var i = 0; i < interfaces.Count; ++i)
        {
            assemblyMetadata.InterfacesMetaData[i] = new InterfaceMetaData(interfaces[i]);
        }
    }
}