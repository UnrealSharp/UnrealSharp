using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UnrealSharp.SourceGenerators;

[Generator]
public class DelegateWrapperGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // No initialization required.
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var syntaxTrees = context.Compilation.SyntaxTrees;

        foreach (var tree in syntaxTrees)
        {
            var semanticModel = context.Compilation.GetSemanticModel(tree);
            var delegateDeclarations = tree.GetRoot().DescendantNodes().OfType<DelegateDeclarationSyntax>();

            foreach (var delegateDeclaration in delegateDeclarations)
            {
                if (semanticModel.GetDeclaredSymbol(delegateDeclaration) is not INamedTypeSymbol delegateSymbol)
                {
                    continue;
                }

                if (delegateSymbol.ContainingType != null)
                {
                    continue;
                }

                string className = $"Event{delegateSymbol.Name}";
                    
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("using UnrealSharp;");
                stringBuilder.AppendLine("using UnrealSharp.Interop;");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"namespace {delegateSymbol.ContainingNamespace.ToDisplayString()};");
                stringBuilder.AppendLine();
                
                stringBuilder.AppendLine($"public class {className} : EventDispatcher");
                stringBuilder.AppendLine("{");
                
                stringBuilder.AppendLine($"    public {className}(IntPtr nativeProperty, UnrealSharpObject owner) : base(nativeProperty, owner)");
                stringBuilder.AppendLine("    {");
                stringBuilder.AppendLine("    }");
                stringBuilder.AppendLine();
                    
                GenerateAddFunction(stringBuilder, delegateSymbol, className);
                GenerateRemoveFunction(stringBuilder, delegateSymbol, className);
                GenerateBroadcastFunction(stringBuilder, delegateSymbol);
                    
                stringBuilder.AppendLine("}");
                    
                string source = stringBuilder.ToString();
                context.AddSource($"Event{delegateSymbol.Name}.cs", SourceText.From(source, Encoding.UTF8));
            }
        }
    }

    void GenerateAddFunction(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol, string className)
    {
        var delegateInvokeMethod = delegateSymbol.DelegateInvokeMethod;

        if (delegateInvokeMethod == null)
        {
            return;
        }
        
        if (delegateInvokeMethod.Parameters.IsEmpty)
        {
            stringBuilder.AppendLine($"    public void Add(Action action)");
        }
        else
        {
            stringBuilder.Append("    public void Add(Action<");
            stringBuilder.Append(string.Join(", ", delegateInvokeMethod.Parameters.Select(x => $"{x.Type}")));
            stringBuilder.Append("> action)");
            stringBuilder.AppendLine();
        }
        
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine("        try");
        stringBuilder.AppendLine("        {");
        CastToUnrealSharpObject(stringBuilder);
        stringBuilder.AppendLine($"            FMulticastDelegatePropertyExporter.CallAddDelegate(_nativeDelegate, _owner.NativeObject, unrealSharpObject.NativeObject, action.Method.Name);");
        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine("        catch (Exception ex)");
        stringBuilder.AppendLine("        {");
        stringBuilder.AppendLine("            Console.WriteLine(ex);");
        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine();
    }

    void GenerateRemoveFunction(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol, string className)
    {
        var delegateInvokeMethod = delegateSymbol.DelegateInvokeMethod;

        if (delegateInvokeMethod == null)
        {
            return;
        }
        
        if (delegateInvokeMethod.Parameters.IsEmpty)
        {
            stringBuilder.AppendLine($"    public void Remove(Action action)");
        }
        else
        {
            stringBuilder.Append("    public void Remove(Action<");
            stringBuilder.Append(string.Join(", ", delegateInvokeMethod.Parameters.Select(x => $"{x.Type}")));
            stringBuilder.Append("> action)");
            stringBuilder.AppendLine();
        }
        
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine("        try");
        stringBuilder.AppendLine("        {");
        CastToUnrealSharpObject(stringBuilder);
        stringBuilder.AppendLine($"            FMulticastDelegatePropertyExporter.CallRemoveDelegate(_nativeDelegate, _owner.NativeObject, unrealSharpObject.NativeObject, action.Method.Name);");
        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine("        catch (Exception ex)");
        stringBuilder.AppendLine("        {");
        stringBuilder.AppendLine("            Console.WriteLine(ex);");
        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine();
    }

    void GenerateBroadcastFunction(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol)
    {
        var delegateInvokeMethod = delegateSymbol.DelegateInvokeMethod;
        
        if (delegateInvokeMethod.Parameters.Length == 0)
        {
            stringBuilder.AppendLine($"    public void Broadcast()");
        }
        else
        {
            stringBuilder.Append($"    public void Broadcast(");
            stringBuilder.Append(string.Join(", ", delegateInvokeMethod.Parameters.Select(x => $"{x.Type} {x.Name}")));
            stringBuilder.AppendLine(")");
        }
                    
        stringBuilder.AppendLine("    {");

        if (delegateInvokeMethod.Parameters.Length == 0)
        {
            stringBuilder.AppendLine("        FMulticastDelegatePropertyExporter.CallBroadcastDelegate(_nativeDelegate, _owner.NativeObject, IntPtr.Zero);");
        }
        else
        {
            stringBuilder.AppendLine("        // The weaver will implement marshalling here."); 
        }

        stringBuilder.AppendLine("    }");
    }

    void CastToUnrealSharpObject(StringBuilder stringBuilder)
    {
        stringBuilder.AppendLine("            UnrealSharpObject unrealSharpObject = (UnrealSharpObject) action.Target;");
        stringBuilder.AppendLine("            if (unrealSharpObject == null)");
        stringBuilder.AppendLine("            {");
        stringBuilder.AppendLine("                return;");
        stringBuilder.AppendLine("            }");
    }
}