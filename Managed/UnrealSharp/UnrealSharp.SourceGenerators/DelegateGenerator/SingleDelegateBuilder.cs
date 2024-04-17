using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.SourceGenerators;

public class SingleDelegateBuilder : DelegateBuilder
{
    public override void StartBuilding(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol, INamedTypeSymbol classSymbol)
    {
        GenerateAddOperator(stringBuilder, delegateSymbol, classSymbol.Name);

        GenerateGetInvoker(stringBuilder, delegateSymbol);

        GenerateRemoveOperator(stringBuilder, delegateSymbol, classSymbol.Name);

        GenerateConstructors(stringBuilder, classSymbol);

        //Check if the class has an Invoker method already
        if (!classSymbol.GetMembers("Invoker").Any())
        {
            GenerateInvoke(stringBuilder, delegateSymbol);
        }
    }
    
    void GenerateConstructors(StringBuilder stringBuilder, INamedTypeSymbol classSymbol)
    {
        stringBuilder.AppendLine($"    public {classSymbol.Name}() : base()");
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine();

        stringBuilder.AppendLine($"    public {classSymbol.Name}(DelegateData data) : base(data)");
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine();
        
        stringBuilder.AppendLine($"    public {classSymbol.Name}(UnrealSharp.CoreUObject.Object targetObject, Name functionName) : base(targetObject, functionName)");
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