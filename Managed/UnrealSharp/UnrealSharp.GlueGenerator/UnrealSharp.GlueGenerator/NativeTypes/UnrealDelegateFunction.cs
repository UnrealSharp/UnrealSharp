using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public record UnrealDelegateFunction : UnrealFunction
{
    public UnrealDelegateFunction(SemanticModel model, ISymbol typeSymbol, MethodDeclarationSyntax syntax, UnrealType outer) : base(model, typeSymbol, syntax, outer)
    {
    }

    public UnrealDelegateFunction(EFunctionFlags flags, string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName, UnrealType? outer = null) : base(flags, sourceName, typeNameSpace, accessibility, assemblyName, outer)
    {
    }

    public UnrealDelegateFunction(SemanticModel model, ISymbol typeSymbol, DelegateDeclarationSyntax syntax, UnrealType outer) : base(model, typeSymbol, syntax, outer)
    {
    }
    
    public override string EngineName => SourceName.Substring(1);
}