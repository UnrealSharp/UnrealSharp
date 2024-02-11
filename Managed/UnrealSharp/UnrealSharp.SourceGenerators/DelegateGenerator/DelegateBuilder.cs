using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.SourceGenerators;

public abstract class DelegateBuilder
{
    public abstract void StartBuilding(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol, INamedTypeSymbol classSymbol);
    
    protected void GenerateGetInvoker(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol)
    {
        stringBuilder.AppendLine($"    protected override {delegateSymbol} GetInvoker()");
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine("        return Invoker;");
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine();
    }
    
    protected void GenerateInvoke(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol)
    {
        if (delegateSymbol.DelegateInvokeMethod == null)
        {
            return;
        }
        
        if (delegateSymbol.DelegateInvokeMethod.Parameters.IsEmpty)
        {
            stringBuilder.AppendLine($"    protected void Invoker()");
        }
        else
        {
            stringBuilder.Append($"    protected void Invoker(");
            stringBuilder.Append(string.Join(", ", delegateSymbol.DelegateInvokeMethod.Parameters.Select(x => $"{x.Type} {x.Name}")));
            stringBuilder.Append(")");
            stringBuilder.AppendLine();
        }
        
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine("       ProcessDelegate(IntPtr.Zero);");
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine();
    }
}