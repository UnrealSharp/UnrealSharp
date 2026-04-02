using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public abstract record UnrealClassBase : UnrealStruct
{
    protected readonly EquatableList<UnrealFunctionBase> Functions = new(new List<UnrealFunctionBase>());
    protected readonly EquatableList<UnrealFunctionBase> AsyncFunctions = new(new List<UnrealFunctionBase>());

    public EClassFlags ClassFlags = EClassFlags.CompiledFromBlueprint;
    
    protected UnrealClassBase(ITypeSymbol typeSymbol, UnrealType? outer = null) : base(typeSymbol, outer)
    {

    }
    
    public UnrealClassBase(string parentName, string parentNamespace, string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName, UnrealType? outer = null) 
        : base(sourceName, typeNameSpace, accessibility, assemblyName, outer)
    {

    }

    public void AddFunction(UnrealFunctionBase function)
    {
        Functions.List.Add(function);
    }
    
    public void AddAsyncFunction(UnrealFunctionBase function)
    {
        AsyncFunctions.List.Add(function);
    }

    public override void PopulateJsonObject(JsonWriter jsonWriter)
    {
        base.PopulateJsonObject(jsonWriter);
        Functions.PopulateJsonWithArray(jsonWriter, "Functions");
    }
}