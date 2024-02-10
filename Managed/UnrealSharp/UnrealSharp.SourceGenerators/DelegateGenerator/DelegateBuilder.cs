using System.Text;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.SourceGenerators;

public abstract class DelegateBuilder
{
    public abstract void StartBuilding(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol, INamedTypeSymbol classSymbol);
}