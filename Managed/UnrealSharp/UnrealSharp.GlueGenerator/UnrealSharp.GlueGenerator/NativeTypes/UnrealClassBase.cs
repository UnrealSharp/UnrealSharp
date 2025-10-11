using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public abstract record UnrealClassBase : UnrealStruct
{
    protected readonly EquatableList<UnrealFunctionBase> Functions = new(new List<UnrealFunctionBase>());
    protected readonly EquatableList<UnrealFunctionBase> AsyncFunctions = new(new List<UnrealFunctionBase>());

    public EClassFlags ClassFlags = EClassFlags.CompiledFromBlueprint;

    public readonly string ParentName = string.Empty;
    public readonly string ParentNamespace = string.Empty;
    public string FullParentName => string.IsNullOrEmpty(ParentNamespace) ? ParentName : ParentNamespace + "." + ParentName;
    
    protected UnrealClassBase(ITypeSymbol typeSymbol, SyntaxNode syntax, UnrealType? outer = null) : base(typeSymbol, syntax, outer)
    {
        if (typeSymbol.BaseType is null)
        {
            return;
        }
        
        ParentName = typeSymbol.BaseType!.Name;
        ParentNamespace = typeSymbol.BaseType.ContainingNamespace.ToDisplayString();
    }
    
    public UnrealClassBase(string parentName, string parentNamespace, string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName, UnrealType? outer = null) 
        : base(sourceName, typeNameSpace, accessibility, assemblyName, outer)
    {
        ParentName = parentName;
        ParentNamespace = parentNamespace;
    }
    
    public void AddFunction(UnrealFunctionBase function)
    {
        Functions.List.Add(function);
    }
    
    public void AddAsyncFunction(UnrealFunctionBase function)
    {
        AsyncFunctions.List.Add(function);
    }

    public override void CreateTypeBuilder(GeneratorStringBuilder builder)
    {
        base.CreateTypeBuilder(builder);
        AppendFunctions(builder, Functions);
    }
    
    protected void AppendFunctions(GeneratorStringBuilder builder, EquatableList<UnrealFunctionBase> functions)
    {
        if (functions.Count == 0)
        {
            return;
        }
        
        builder.AppendLine($"InitFunctions({BuilderNativePtr}, {functions.Count});");
            
        foreach (UnrealFunctionBase function in functions.List)
        {
            function.CreateTypeBuilder(builder);
        }
    }
}