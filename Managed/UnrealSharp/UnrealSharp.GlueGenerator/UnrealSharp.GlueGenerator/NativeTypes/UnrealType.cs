using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public record struct MetaDataInfo
{
    public string Key;
    public string Value;
    
    public MetaDataInfo(string inKey, string inValue)
    {
        Key = inKey;
        Value = inValue;
    }
}

public record UnrealType
{
    public UnrealType? Outer;
    public bool HasOuter { get; }

    public string SourceName = string.Empty;
    public virtual string EngineName => SourceName;
    public readonly string AssemblyName = string.Empty;
    
    public string FullName => Namespace + "." + SourceName;
    public string Namespace = string.Empty;
    
    public Accessibility Protection;
    
    public virtual int FieldTypeValue => throw new NotImplementedException();
    
    public string BuilderNativePtr => HasOuter ? $"{Outer!.SourceName}_{SourceName}Ptr" : $"{SourceName}Ptr";
    
    private readonly EquatableList<MetaDataInfo> _metaData = new(new List<MetaDataInfo>());
    
    public UnrealType(ISymbol? memberSymbol, SyntaxNode syntaxNode, UnrealType? outer = null)
    {
        Outer = outer;
        HasOuter = outer != null;
        
        if (memberSymbol != null)
        {
            _metaData = new EquatableList<MetaDataInfo>(memberSymbol.GetUMetaAttributes());
            Namespace = memberSymbol.ContainingNamespace.ToDisplayString();
            SourceName = memberSymbol.Name;
            AssemblyName = memberSymbol.ContainingAssembly.Name;
        }
        
        if (syntaxNode is MemberDeclarationSyntax typeDeclaration)
        {
            string firstModifier = typeDeclaration.Modifiers.Count > 0 ? typeDeclaration.Modifiers[0].Text : string.Empty;
            
            if (string.IsNullOrEmpty(firstModifier) || firstModifier is not ("public" or "private" or "protected"))
            {
                Protection = Accessibility.NotApplicable;
                return;
            }
            
            Protection = firstModifier switch
            {
                "public" => Accessibility.Public,
                "private" => Accessibility.Private,
                "protected" => Accessibility.Protected,
                "internal" => Accessibility.Internal,
                "protected internal" => Accessibility.ProtectedOrInternal,
                _ => Accessibility.NotApplicable
            };
        }
    }

    public UnrealType(string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName, UnrealType? outer = null)
    {
        SourceName = sourceName;
        Namespace = typeNameSpace;
        Protection = accessibility;
        AssemblyName = assemblyName;
        Outer = outer;
        HasOuter = outer != null;
    }
    
    public void AddMetaData(string key, string value) => _metaData.List.Add(new MetaDataInfo { Key = key, Value = value });
    public void AddMetaDataRange(IEnumerable<MetaDataInfo> metaData) => _metaData.List.AddRange(metaData);
    
    public EquatableList<MetaDataInfo> MetaData => _metaData;
    public bool HasAnyMetaData => _metaData.List.Count > 0;
    
    public virtual void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc) { }
    
    public virtual void CreateTypeBuilder(GeneratorStringBuilder builder)
    {
        AppendMetaData(builder, BuilderNativePtr);
    }
    
    protected void AppendMetaData(GeneratorStringBuilder builder, string ownerPtr)
    {
        if (!HasAnyMetaData)
        {
            return;
        }
        
        builder.AppendLine($"InitMetaData({ownerPtr}, {_metaData.Count});");
        
        foreach (MetaDataInfo metaData in _metaData.List)
        {
            builder.AppendLine($"AddMetaData({ownerPtr}, \"{metaData.Key}\", \"{metaData.Value}\");");
        }
    }

    public override string ToString()
    {
        return $"{SourceName} (Outer: {Outer?.SourceName ?? "None"})";
    }

    public virtual bool Equals(UnrealType? other)
    {
        if (other is null)
        {
            return false;
        }
        
        return SourceName == other.SourceName && 
               _metaData.Equals(other._metaData) && 
               Namespace == other.Namespace && 
               Protection == other.Protection;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = SourceName.GetHashCode();
            hashCode = (hashCode * 397) ^ Namespace.GetHashCode();
            hashCode = (hashCode * 397) ^ _metaData.GetHashCode();
            return hashCode;
        }
    }
}