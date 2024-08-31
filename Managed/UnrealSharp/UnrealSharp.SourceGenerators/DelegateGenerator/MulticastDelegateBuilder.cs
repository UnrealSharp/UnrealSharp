using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using UnrealSharp.SourceGenerators.DelegateGenerator;

namespace UnrealSharp.SourceGenerators;

public class MulticastDelegateBuilder : DelegateBuilder
{
    public override void StartBuilding(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol, string className, bool generateInvoker)
    {
        GenerateAddOperator(stringBuilder, delegateSymbol, className);
        GenerateGetInvoker(stringBuilder, delegateSymbol);
        GenerateRemoveOperator(stringBuilder, delegateSymbol, className);
        
        //Check if the class has an Invoker method already
        if (generateInvoker)
        {
            GenerateInvoke(stringBuilder, delegateSymbol);
        }
    }
    
    void GenerateAddOperator(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol, string className)
    {
        stringBuilder.AppendLine($"    public static {className} operator +({className} thisDelegate, {delegateSymbol.Name} handler)");
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine("        thisDelegate.Add(handler);");
        stringBuilder.AppendLine("        return thisDelegate;");
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine();
    }
    
    void GenerateRemoveOperator(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol, string className)
    {
        stringBuilder.AppendLine($"    public static {className} operator -({className} thisDelegate, {delegateSymbol.Name} handler)");
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine("        thisDelegate.Remove(handler);");
        stringBuilder.AppendLine("        return thisDelegate;");
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine();
    }
}