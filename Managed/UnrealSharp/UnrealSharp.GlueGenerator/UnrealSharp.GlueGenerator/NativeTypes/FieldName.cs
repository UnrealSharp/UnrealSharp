using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

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

    public void SerializeToJson(JsonWriter wtr, bool stripPrefix = false)
    {
        if (string.IsNullOrEmpty(Name))
        {
            wtr.WriteNull();
            return;
        }

        wtr.WriteStartObject();
        wtr.WritePropertyName("Name");
        wtr.WriteValue(stripPrefix ? Name.Substring(1) : Name);
        wtr.WritePropertyName("Namespace");
        wtr.WriteValue(Namespace);
        wtr.WritePropertyName("AssemblyName");
        wtr.WriteValue(Assembly);
        wtr.WriteEndObject();
    }
    
    public void SerializeToJson(JsonWriter jsonWriter, string propertyName, bool stripPrefix = false)
    {
        if (string.IsNullOrEmpty(Name))
        {
            return;
        }

        jsonWriter.WritePropertyName(propertyName);
        SerializeToJson(jsonWriter, stripPrefix);
    }
}