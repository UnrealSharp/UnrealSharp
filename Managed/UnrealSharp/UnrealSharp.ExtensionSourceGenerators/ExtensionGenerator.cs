using System.Text;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.ExtensionSourceGenerators;

public abstract class ExtensionGenerator
{
    public abstract void Generate(ref StringBuilder builder, INamedTypeSymbol classSymbol);
}