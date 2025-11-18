using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public record UnrealDelegateFunction : UnrealFunction
{
    public UnrealDelegateFunction(SemanticModel model, IMethodSymbol typeSymbol, UnrealType outer) : base(model, typeSymbol, outer)
    {
    }

    public UnrealDelegateFunction(EFunctionFlags flags, string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName, UnrealType? outer = null) : base(flags, sourceName, typeNameSpace, accessibility, assemblyName, outer)
    {
    }
    
    public override string EngineName => SourceName.Substring(1);
}