using System.Collections.Generic;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public abstract record UnrealClassBase : UnrealStruct
{
    protected readonly EquatableList<UnrealFunctionBase> Functions = new(new List<UnrealFunctionBase>());
    protected readonly EquatableList<UnrealFunctionBase> AsyncFunctions = new(new List<UnrealFunctionBase>());

    public EClassFlags ClassFlags = EClassFlags.CompiledFromBlueprint;

    public FieldName ParentClass;
    
    public string FullParentName => string.IsNullOrEmpty(ParentClass.Namespace) ? ParentClass.Name : $"{ParentClass.Namespace}.{ParentClass.Name}";
    
    protected UnrealClassBase(ITypeSymbol typeSymbol, SyntaxNode syntax, UnrealType? outer = null) : base(typeSymbol, syntax, outer)
    {
        if (typeSymbol.BaseType is not null)
        {
            ParentClass = new FieldName(typeSymbol.BaseType);
        }
    }
    
    public UnrealClassBase(string parentName, string parentNamespace, string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName, UnrealType? outer = null) 
        : base(sourceName, typeNameSpace, accessibility, assemblyName, outer)
    {
        ParentClass = new FieldName(parentName, parentNamespace, assemblyName);
    }
    
    public void AddFunction(UnrealFunctionBase function)
    {
        Functions.List.Add(function);
    }
    
    public void AddAsyncFunction(UnrealFunctionBase function)
    {
        AsyncFunctions.List.Add(function);
    }

    public override void PopulateJsonObject(JsonObject jsonObject)
    {
        base.PopulateJsonObject(jsonObject);
        Functions.PopulateWithArray(jsonObject, "Functions");
        ParentClass.SerializeToJson(jsonObject, "ParentClass", true);
    }
}