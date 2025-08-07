using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace UnrealSharpWeaver.Utilities;

public static class AssemblyUtilities
{
    public static TypeReference? FindGenericType(this AssemblyDefinition assembly, string typeNamespace, string typeName, TypeReference[] typeParameters, bool bThrowOnException = true)
    {
        TypeReference? typeRef = FindType(assembly, typeName, typeNamespace, bThrowOnException);
        return typeRef == null ? null : typeRef.Resolve().MakeGenericInstanceType(typeParameters).ImportType();
    }

    public static TypeReference? FindType(this AssemblyDefinition assembly, string typeName, string typeNamespace = "", bool throwOnException = true)
    {
        foreach (var module in assembly.Modules)
        {
            foreach (var type in module.GetAllTypes())
            {
                if ((typeNamespace.Length > 0 && type.Namespace != typeNamespace) || type.Name != typeName)
                {
                    continue;
                }
                
                return type.ImportType();
            }
        }

        if (throwOnException)
        {
            throw new TypeAccessException($"Type \"{typeNamespace}.{typeName}\" not found in userAssembly {assembly.Name}");
        }

        return null;
    }
    
    public static TypeDefinition CreateNewClass(this AssemblyDefinition assembly, string classNamespace, string className, TypeAttributes attributes, TypeReference? parentClass = null)
    {
        if (parentClass == null)
        {
            parentClass = assembly.MainModule.TypeSystem.Object;
        }
        
        TypeDefinition newType = new TypeDefinition(classNamespace, className, attributes, parentClass);
        assembly.MainModule.Types.Add(newType);
        return newType;
    }
    
    public static void ForEachAssembly(Func<AssemblyDefinition, bool> action)
    {
        List<AssemblyDefinition> assemblies = [WeaverImporter.Instance.UnrealSharpAssembly, WeaverImporter.Instance.UnrealSharpCoreAssembly];
        assemblies.AddRange(WeaverImporter.Instance.AllProjectAssemblies);
        
        foreach (AssemblyDefinition assembly in assemblies)
        {
            if (!action(assembly))
            {
                return;
            }
        }
    }
}