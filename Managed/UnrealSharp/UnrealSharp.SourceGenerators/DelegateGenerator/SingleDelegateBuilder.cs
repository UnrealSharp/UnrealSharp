using System.Text;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.SourceGenerators;

public class SingleDelegateBuilder : DelegateBuilder
{
    public override void StartBuilding(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol,
        INamedTypeSymbol classSymbol)
    {
        throw new System.NotImplementedException();
    }
}