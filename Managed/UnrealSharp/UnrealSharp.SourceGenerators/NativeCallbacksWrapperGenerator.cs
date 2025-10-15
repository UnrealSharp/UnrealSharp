using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using UnrealSharp.SourceGenerator.Utilities;

namespace UnrealSharp.SourceGenerators;

public struct ParameterInfo
{
    public DelegateParameterInfo Parameter { get; set; }
}

[Generator]
public class NativeCallbacksWrapperGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
                static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0,
                static (syntaxContext, _) => GetClassInfoOrNull(syntaxContext));

        var classAndCompilation = classDeclarations.Combine(context.CompilationProvider);

        context.RegisterSourceOutput(classAndCompilation, (spc, pair) =>
        {
            var maybeClassInfo = pair.Left; // ClassInfo?
            var compilation = pair.Right;
            if (!maybeClassInfo.HasValue)
            {
                return;
            }
            GenerateForClass(spc, compilation, maybeClassInfo.Value);
        });
    }

    private static void GenerateForClass(SourceProductionContext context, Compilation compilation, ClassInfo classInfo)
    {
        var model = compilation.GetSemanticModel(classInfo.ClassDeclaration.SyntaxTree);
        var sourceBuilder = new StringBuilder();

        HashSet<string> namespaces = [];
        foreach (DelegateInfo delegateInfo in classInfo.Delegates)
        {
            foreach (var parameter in delegateInfo.ParametersAndReturnValue)
            {
                var typeInfo = model.GetTypeInfo(parameter.Type);
                var typeSymbol = typeInfo.Type;

                if (typeSymbol == null || typeSymbol.ContainingNamespace == null)
                {
                    continue;
                }

                if (typeSymbol is INamedTypeSymbol nts && nts.IsGenericType)
                {
                    namespaces.UnionWith(nts.TypeArguments.Where(t => t.ContainingNamespace != null).Select(t => t.ContainingNamespace!.ToDisplayString()));
                }

                namespaces.Add(typeSymbol.ContainingNamespace.ToDisplayString());
            }
        }

        if (classInfo.NullableAwareable)
        {
            sourceBuilder.AppendLine("#nullable enable");
        }
        else
        {
            sourceBuilder.AppendLine("#nullable disable");
        }
        sourceBuilder.AppendLine("#pragma warning disable CS8500, CS0414");
        sourceBuilder.AppendLine();

        foreach (string? ns in namespaces)
        {
            if (string.IsNullOrWhiteSpace(ns)) continue;
            sourceBuilder.AppendLine($"using {ns};");
        }

        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"namespace {classInfo.Namespace}");
        sourceBuilder.AppendLine("{");
        sourceBuilder.AppendLine($"    public static unsafe partial class {classInfo.Name}");
        sourceBuilder.AppendLine("    {");

        sourceBuilder.AppendLine("        static " + classInfo.Name + "()");
        sourceBuilder.AppendLine("        {");

        foreach (DelegateInfo delegateInfo in classInfo.Delegates)
        {
            string delegateName = delegateInfo.Name;

            string totalSizeDelegateName = delegateName + "TotalSize";
            if (!delegateInfo.HasReturnValue && delegateInfo.Parameters.Count == 0)
            {
                sourceBuilder.AppendLine($"             int {totalSizeDelegateName} = 0;");
            }
            else
            {
                sourceBuilder.Append($"             int {totalSizeDelegateName} = ");

                void AppendSizeOf(DelegateParameterInfo param)
                {
                    string typeFullName = param.Type.GetAnnotatedTypeName(model) ?? param.Type.ToString();

                    if (param.IsOutParameter || param.IsRefParameter)
                    {
                        sourceBuilder.Append($"IntPtr.Size");
                    }
                    else
                    {
                        sourceBuilder.Append($"sizeof({typeFullName})");
                    }
                }

                List<DelegateParameterInfo> parameters = delegateInfo.ParametersAndReturnValue;

                for (int i = 0; i < parameters.Count; i++)
                {
                    AppendSizeOf(parameters[i]);

                    if (i != parameters.Count - 1)
                    {
                        sourceBuilder.Append(" + ");
                    }
                }

                sourceBuilder.AppendLine(";");
            }

            string funcPtrName = delegateName + "FuncPtr";
            sourceBuilder.AppendLine($"             IntPtr {funcPtrName} = UnrealSharp.Binds.NativeBinds.TryGetBoundFunction(\"{classInfo.Name}\", \"{delegateInfo.Name}\", {totalSizeDelegateName});");
            sourceBuilder.Append($"             {delegateName} = (delegate* unmanaged<");
            sourceBuilder.Append(string.Join(", ", delegateInfo.Parameters.Select(p =>
            {
                string prefix = p.IsOutParameter ? "out " : p.IsRefParameter ? "ref " : string.Empty;
                return prefix + (p.Type.GetAnnotatedTypeName(model) ?? p.Type.ToString());
            })));

            if (delegateInfo.Parameters.Count > 0)
            {
                sourceBuilder.Append(", ");
            }
            
            sourceBuilder.Append(delegateInfo.ReturnValue.Type.GetAnnotatedTypeName(model) ?? delegateInfo.ReturnValue.Type.ToString());

            sourceBuilder.Append($">){funcPtrName};");
            sourceBuilder.AppendLine();
        }

        sourceBuilder.AppendLine("        }");

        foreach (DelegateInfo delegateInfo in classInfo.Delegates)
        {
            string returnTypeFullName = delegateInfo.ReturnValue.Type.GetAnnotatedTypeName(model) ?? delegateInfo.ReturnValue.Type.ToString();
            sourceBuilder.AppendLine($"        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
            sourceBuilder.Append($"        public static {returnTypeFullName} Call{delegateInfo.Name}(");

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

                string typeFullName = parameter.Type.GetAnnotatedTypeName(model) ?? parameter.Type.ToString();
                sourceBuilder.Append($"{typeFullName} {parameter.Name}");
            }

            sourceBuilder.AppendLine(")");
            sourceBuilder.AppendLine("        {");

            string delegateName = delegateInfo.Name;

            if (delegateInfo.ReturnValue.Type.ToString() != "void")
            {
                sourceBuilder.Append($"            return {delegateName}(");
            }
            else
            {
                sourceBuilder.Append($"            {delegateName}(");
            }

            sourceBuilder.Append(string.Join(", ", delegateInfo.Parameters.Select(p =>
            {
                string prefix = p.IsOutParameter ? "out " : p.IsRefParameter ? "ref " : string.Empty;
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

    private static ClassInfo? GetClassInfoOrNull(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclaration)
        {
            return null;
        }

        // Check attribute (support both NativeCallbacks and NativeCallbacksAttribute)
        bool hasNativeCallbacksAttribute = classDeclaration.AttributeLists
            .SelectMany(a => a.Attributes)
            .Any(a => a.Name.ToString() is "NativeCallbacks" or "NativeCallbacksAttribute");

        if (!hasNativeCallbacksAttribute)
        {
            return null;
        }

        string namespaceName = AnalyzerStatics.GetFullNamespace(classDeclaration);

        if (string.IsNullOrEmpty(namespaceName))
        {
            return null;
        }

        var classInfo = new ClassInfo
        {
            ClassDeclaration = classDeclaration,
            Name = classDeclaration.Identifier.ValueText,
            Namespace = namespaceName,
            Delegates = new List<DelegateInfo>(),
            NullableAwareable = context.SemanticModel.GetNullableContext(context.Node.Span.Start).HasFlag(NullableContext.AnnotationsEnabled)
        };

        foreach (MemberDeclarationSyntax member in classDeclaration.Members)
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

            char paramName = 'a';

            for (int i = 0; i < functionPointerTypeSyntax.ParameterList.Parameters.Count; i++)
            {
                FunctionPointerParameterSyntax param = functionPointerTypeSyntax.ParameterList.Parameters[i];

                DelegateParameterInfo parameter = new DelegateParameterInfo
                {
                    Name = paramName.ToString(),
                    IsOutParameter = param.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.OutKeyword)),
                    IsRefParameter = param.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.RefKeyword)),
                    Type = param.Type,
                };

                bool isReturnParameter = i == functionPointerTypeSyntax.ParameterList.Parameters.Count - 1;
                if (isReturnParameter)
                {
                    delegateInfo.ReturnValue = parameter;
                }
                else
                {
                    delegateInfo.Parameters.Add(parameter);
                }

                paramName++;
            }

            classInfo.Delegates.Add(delegateInfo);
        }

        return classInfo;
    }
}

internal struct ClassInfo
{
    public ClassDeclarationSyntax ClassDeclaration;
    public string Name;
    public string Namespace;
    public List<DelegateInfo> Delegates;
    public bool NullableAwareable;
}

internal struct DelegateInfo
{
    public string Name;
    public List<DelegateParameterInfo> Parameters;
    public List<DelegateParameterInfo> ParametersAndReturnValue
    {
        get
        {
            List<DelegateParameterInfo> allParameters = new List<DelegateParameterInfo>(Parameters);

            if (ReturnValue.Type.ToString() != "void")
            {
                allParameters.Add(ReturnValue);
            }

            return allParameters;
        }
    }

    public bool HasReturnValue => ReturnValue.Type.ToString() != "void";
    public DelegateParameterInfo ReturnValue;
}

public struct DelegateParameterInfo
{
    public string Name;
    public TypeSyntax Type;
    public bool IsOutParameter;
    public bool IsRefParameter;
}