using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using UnrealSharp.SourceGenerators.DelegateGenerator;

namespace UnrealSharp.SourceGenerators;

public class SingleDelegateBuilder : DelegateBuilder
{
    public override void StartBuilding(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol, string className, bool generateInvoker)
    {
        GenerateAddOperator(stringBuilder, delegateSymbol, className);

        GenerateGetInvoker(stringBuilder, delegateSymbol);

        GenerateRemoveOperator(stringBuilder, delegateSymbol, className);

        GenerateConstructors(stringBuilder, className);

        //Check if the class has an Invoker method already
        if (generateInvoker)
        {
            GenerateInvoke(stringBuilder, delegateSymbol);
        }
    }
    
    void GenerateConstructors(StringBuilder stringBuilder, string className)
    {
        stringBuilder.AppendLine($"    public {className}() : base()");
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine();

        stringBuilder.AppendLine($"    public {className}(DelegateData data) : base(data)");
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine();
        
        stringBuilder.AppendLine($"    public {className}(UnrealSharp.CoreUObject.UObject targetObject, FName functionName) : base(targetObject, functionName)");
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine();
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