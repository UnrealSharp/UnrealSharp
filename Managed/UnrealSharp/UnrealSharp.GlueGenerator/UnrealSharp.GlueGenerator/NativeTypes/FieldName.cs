using Microsoft.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public readonly record struct FieldName
{
    public readonly string Name;
    public readonly string Namespace;
    public readonly string Assembly;

    public readonly string FullName;

    public FieldName(ITypeSymbol typeSymbol) : this(typeSymbol.Name, typeSymbol.GetNamespace(), typeSymbol.ContainingAssembly.Name)
    {
    }
    
    public FieldName(string name, string nameSpace, string assembly)
    {
        Name = name;
        Namespace = nameSpace;
        Assembly = assembly;
        
        FullName = $"{Namespace}.{Name}";
    }
    
    public FieldName(UnrealType type) : this(type.SourceName, type.Namespace, type.AssemblyName)
    {
    }
    
    public FieldName(string name)
    {
        Name = name;
        Namespace = string.Empty;
        Assembly = string.Empty;
        
        FullName = Name;
    }

    public override string ToString()
    {
        return FullName;
    }

    public JObject? SerializeToJson(bool stripPrefix = false)
    {
        if (string.IsNullOrEmpty(Name))
        {
            return null;
        }
        
        JObject fieldObject = new()
        {
            ["Name"] = stripPrefix ? Name.Substring(1) : Name,
            ["Namespace"] = Namespace,
            ["AssemblyName"] = Assembly
        };

        return fieldObject;
    }
    
    public void SerializeToJson(JObject jsonObject, string propertyName, bool stripPrefix = false)
    {
        JObject? fieldObject = SerializeToJson(stripPrefix);
        if (fieldObject != null)
        {
            jsonObject[propertyName] = fieldObject;
        }
    }
}