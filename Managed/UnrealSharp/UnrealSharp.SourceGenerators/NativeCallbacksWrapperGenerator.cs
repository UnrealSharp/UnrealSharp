using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UnrealSharp.SourceGenerators;

public struct ParameterInfo
{
    public DelegateParameterInfo Parameter { get; set; }
    
}

[Generator]
public class NativeCallbacksWrapperGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a syntax receiver that will be created for each compilation
        context.RegisterForSyntaxNotifications(() => new NativeCallbacksSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not NativeCallbacksSyntaxReceiver receiver)
        {
            return;
        }

        Compilation compilation = context.Compilation;
        
        foreach (var classInfo in receiver.ClassesWithNativeCallbacks)
        {
            var model = compilation.GetSemanticModel(classInfo.ClassDeclaration.SyntaxTree);
            var sourceBuilder = new StringBuilder();

            HashSet<string> namespaces = [];
            foreach (var delegateInfo in classInfo.Delegates)
            {
                foreach (var parameter in delegateInfo.Parameters)
                {
                    var typeInfo = model.GetTypeInfo(parameter.Type);
                    var typeSymbol = typeInfo.Type;
                    
                    if (typeSymbol == null || typeSymbol.ContainingNamespace == null)
                    {
                        continue;
                    }

                    if (typeSymbol is INamedTypeSymbol nts && nts.IsGenericType)
                    {
                        namespaces.UnionWith(nts.TypeArguments.Select(t => t.ContainingNamespace.ToDisplayString()));
                    }

                    namespaces.Add(typeSymbol.ContainingNamespace.ToDisplayString());
                }
            }

            sourceBuilder.AppendLine("#nullable disable");
            sourceBuilder.AppendLine();

            foreach (var ns in namespaces)
            {
                sourceBuilder.AppendLine($"using {ns};");
            }
            
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($"namespace {classInfo.Namespace}");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine($"    public static unsafe partial class {classInfo.Name}");
            sourceBuilder.AppendLine("    {");

            foreach (var delegateInfo in classInfo.Delegates)
            {
                int lastIndex = delegateInfo.Parameters.Count - 1;
                DelegateParameterInfo returnValueType = delegateInfo.Parameters[lastIndex];
                
                // Remove return value. We don't need it anymore.
                delegateInfo.Parameters.RemoveAt(lastIndex);
                
                sourceBuilder.Append($"        public static {returnValueType.Type.ToString()} Call{delegateInfo.Name}(");

                // Handle parameters
                bool firstParameter = true;

                foreach (DelegateParameterInfo parameter in delegateInfo.Parameters)
                {
                    if (!firstParameter)
                    {
                        sourceBuilder.Append(", ");
                    }

                    firstParameter = false;

                    if (parameter.IsOutParameter)
                    {
                        sourceBuilder.Append("out ");
                    }
                    
                    if (parameter.IsRefParameter)
                    {
                        sourceBuilder.Append("ref ");
                    }
                    
                    string typeFullName = model.GetTypeInfo(parameter.Type).Type.ToDisplayString();
                    sourceBuilder.Append($"{typeFullName} {parameter.Name}");
                }

                sourceBuilder.AppendLine(")");
                sourceBuilder.AppendLine("        {");

                string delegateName = $"{classInfo.Name}.{delegateInfo.Name}";
                
                sourceBuilder.AppendLine($"             if ({delegateName} == null)");
                sourceBuilder.AppendLine("             {");
                
                sourceBuilder.AppendLine("                 int totalSize = 0;");
                
                void AppendSizeOf(TypeSyntax type)
                {
                    string typeFullName = model.GetTypeInfo(type).Type.ToDisplayString();
                    sourceBuilder.AppendLine($"                 totalSize += sizeof({typeFullName});");
                }
                
                foreach (var parameter in delegateInfo.Parameters)
                {
                    AppendSizeOf(parameter.Type);
                }

                if (delegateInfo.HasReturnValue)
                {
                    AppendSizeOf(returnValueType.Type);
                }
                
                sourceBuilder.AppendLine($"                 IntPtr funcPtr = UnrealSharp.Binds.NativeBinds.TryGetBoundFunction(\"{classInfo.Name}\", \"{delegateInfo.Name}\", totalSize);");
                sourceBuilder.AppendLine($"                 {delegateName} = (delegate* unmanaged<");
                sourceBuilder.Append(string.Join(", ", delegateInfo.Parameters.Select(p =>
                {
                    string prefix = "";

                    if (p.IsOutParameter)
                    {
                        prefix = "out ";
                    }
                    else if (p.IsRefParameter)
                    {
                        prefix = "ref ";
                    }

                    return prefix + model.GetTypeInfo(p.Type).Type.ToDisplayString();
                })));
                
                if (delegateInfo.Parameters.Count > 0)
                {
                    sourceBuilder.Append(", ");
                }
                
                sourceBuilder.Append(model.GetTypeInfo(returnValueType.Type).Type.ToDisplayString());
                sourceBuilder.AppendLine(">)funcPtr;");
                
                sourceBuilder.AppendLine("             }");
                sourceBuilder.AppendLine();

                // Method body
                if (returnValueType.Type.ToString() != "void")
                {
                    sourceBuilder.Append($"            return {delegateName}(");
                }
                else
                {
                    sourceBuilder.Append($"            {delegateName}(");
                }

                sourceBuilder.Append(string.Join(", ", delegateInfo.Parameters.Select(p =>
                {
                    string prefix = "";
                    
                    if (p.IsOutParameter)
                    {
                        prefix = "out ";
                    }
                    else if (p.IsRefParameter)
                    {
                        prefix = "ref ";
                    }

                    return prefix + p.Name;
                })));

                sourceBuilder.AppendLine(");");
                sourceBuilder.AppendLine("        }");
            }
            
            // End class definition
            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");

            context.AddSource($"{classInfo.Name}.generated.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }
    }
}

internal class NativeCallbacksSyntaxReceiver : ISyntaxReceiver
{
    public List<ClassInfo> ClassesWithNativeCallbacks { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not ClassDeclarationSyntax classDeclaration)
        {
            return;
        }
        
        bool hasNativeCallbacksAttribute = classDeclaration.AttributeLists
            .SelectMany(a => a.Attributes)
            .Any(a => a.Name.ToString() == "NativeCallbacks");

        if (!hasNativeCallbacksAttribute)
        {
            return;
        }
            
        string namespaceName = null;
        SyntaxNode currentNode = classDeclaration.Parent;
        while (currentNode != null)
        {
            if (currentNode is FileScopedNamespaceDeclarationSyntax namespaceDeclaration)
            {
                namespaceName = namespaceDeclaration.Name.ToString();
                break;
            }
            
            currentNode = currentNode.Parent;
        }

        if (namespaceName == null)
        {
            return;
        }
                
        var classInfo = new ClassInfo
        {
            ClassDeclaration = classDeclaration,
            Name = classDeclaration.Identifier.ValueText,
            Namespace = namespaceName,
            Delegates = new List<DelegateInfo>()
        };

        // Find all delegate members in the class
        foreach (var member in classDeclaration.Members)
        {
            if (member is not FieldDeclarationSyntax fieldDeclaration ||
                fieldDeclaration.Declaration.Type is not FunctionPointerTypeSyntax functionPointerTypeSyntax)
            {
                continue;
            }
            
            var delegateInfo = new DelegateInfo
            {
                Name = fieldDeclaration.Declaration.Variables.First().Identifier.ValueText,
                Parameters = new List<DelegateParameterInfo>()
            };

            // Iterate through each parameter in the function pointer type syntax
            char paramName = 'a';
            
            for (int i = 0; i < functionPointerTypeSyntax.ParameterList.Parameters.Count; i++)
            {
                FunctionPointerParameterSyntax param = functionPointerTypeSyntax.ParameterList.Parameters[i];
                
                bool isReturnParameter = i == functionPointerTypeSyntax.ParameterList.Parameters.Count - 1;
                if (isReturnParameter && param.Type.ToString() != "void")
                {
                    delegateInfo.HasReturnValue = true;
                }
                
                // Create a new parameter info object
                var parameter = new DelegateParameterInfo
                {
                    // Check if the parameter has the 'out' modifier
                    Name = paramName.ToString(),
                    IsOutParameter = param.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.OutKeyword)),
                    IsRefParameter = param.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.RefKeyword)),
                    Type = param.Type,
                };
                            
                delegateInfo.Parameters.Add(parameter);
                paramName++;
            }

            classInfo.Delegates.Add(delegateInfo);
        }
                
        ClassesWithNativeCallbacks.Add(classInfo);
    }
}

internal struct ClassInfo
{
    public ClassDeclarationSyntax ClassDeclaration;
    public string Name;
    public string Namespace;
    public List<DelegateInfo> Delegates;
}

internal struct DelegateInfo
{
    public string Name;
    public List<DelegateParameterInfo> Parameters;
    public bool HasReturnValue;
}

public struct DelegateParameterInfo
{
    public string Name;
    public TypeSyntax Type;
    public bool IsOutParameter;
    public bool IsRefParameter;
}