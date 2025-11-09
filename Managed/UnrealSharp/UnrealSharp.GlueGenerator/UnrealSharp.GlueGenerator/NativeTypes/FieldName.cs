using System;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public readonly record struct FieldName
{
    public readonly string Name;
    public readonly string Namespace;
    public readonly string Assembly;

    public readonly string FullName;

    public FieldName(ITypeSymbol typeSymbol) : this(typeSymbol.Name, typeSymbol.ContainingNamespace.ToDisplayString(), typeSymbol.ContainingAssembly.Name)
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

    public JsonObject? SerializeToJson(bool stripPrefix = false)
    {
        if (string.IsNullOrEmpty(Name))
        {
            return null;
        }
        
        JsonObject fieldObject = new()
        {
            ["Name"] = stripPrefix ? Name.Substring(1) : Name,
            ["Namespace"] = Namespace,
            ["AssemblyName"] = Assembly
        };

        return fieldObject;
    }
    
    public void SerializeToJson(JsonObject jsonObject, string propertyName, bool stripPrefix = false)
    {
        JsonObject? fieldObject = SerializeToJson(stripPrefix);
        if (fieldObject != null)
        {
            jsonObject[propertyName] = fieldObject;
        }
    }
}