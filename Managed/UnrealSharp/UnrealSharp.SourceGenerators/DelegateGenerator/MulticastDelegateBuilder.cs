using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.SourceGenerators;

public class MulticastDelegateBuilder : DelegateBuilder
{
    public override void StartBuilding(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol, INamedTypeSymbol classSymbol)
    {
        GenerateAddOperator(stringBuilder, delegateSymbol, classSymbol.Name);
            
        GenerateGetInvoker(stringBuilder, delegateSymbol);
            
        GenerateRemoveOperator(stringBuilder, delegateSymbol, classSymbol.Name);
        
        //Check if the class has an Invoker method already
        if (!classSymbol.GetMembers("Invoker").Any())
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