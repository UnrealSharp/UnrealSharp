using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UnrealSharp.SourceGenerators;

internal readonly record struct AsyncMethodInfo(
    ClassDeclarationSyntax ParentClass,
    MethodDeclarationSyntax Method,
    string Namespace,
    TypeSyntax? ReturnType,
    IReadOnlyDictionary<string, string> Metadata,
    bool NullableAwareable,
    bool ReturnsValueTask = false);

[Generator]
public class AsyncWrapperGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var asyncMethods = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is MethodDeclarationSyntax { Parent: ClassDeclarationSyntax } m && m.AttributeLists.Count > 0,
                static (syntaxContext, _) => GetAsyncMethodInfo(syntaxContext))
            .Where(static m => m.HasValue)
            .Select(static (m, _) => m!.Value);

        var asyncMethodsWithCompilation = asyncMethods.Combine(context.CompilationProvider);

        context.RegisterSourceOutput(asyncMethodsWithCompilation, static (spc, pair) =>
        {
            var methodInfo = pair.Left;
            var compilation = pair.Right;
            var source = Generate(methodInfo, compilation);
            if (!string.IsNullOrEmpty(source))
            {
                spc.AddSource($"{methodInfo.ParentClass.Identifier.Text}.{methodInfo.Method.Identifier.Text}.generated.cs", SourceText.From(source, Encoding.UTF8));
            }
        });
    }

    private static string Generate(AsyncMethodInfo asyncMethodInfo, Compilation compilation)
    {
        var model = compilation.GetSemanticModel(asyncMethodInfo.Method.SyntaxTree);
        var method = asyncMethodInfo.Method;

        var cancellationTokenType = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
        ParameterSyntax? cancellationTokenParameter = null;

        HashSet<string> namespaces = new() { "UnrealSharp", "UnrealSharp.Attributes", "UnrealSharp.UnrealSharpCore" };
        foreach (var parameter in method.ParameterList.Parameters)
        {
            if (parameter.Type == null)
            {
                continue;
            }
            var typeInfo = model.GetTypeInfo(parameter.Type);
            var typeSymbol = typeInfo.Type;
            if (SymbolEqualityComparer.Default.Equals(typeSymbol, cancellationTokenType))
            {
                cancellationTokenParameter = parameter;
            }

            if (typeSymbol == null || typeSymbol.ContainingNamespace == null)
            {
                continue;
            }

            if (typeSymbol is INamedTypeSymbol nts && nts.IsGenericType)
            {
                namespaces.UnionWith(nts.TypeArguments.Select(t => t.ContainingNamespace.ToDisplayString()));
            }

                namespaces.Add(typeSymbol.ContainingNamespace.ToDisplayString());
                
                namespaces.UnionWith(parameter.AttributeLists.SelectMany(a => a.Attributes)
                    .Select(a => model.GetTypeInfo(a).Type)
                    .Where(type => type is not null)
                    .Where(type => type!.ContainingNamespace is not null)
                    .Select(type => type!.ContainingNamespace.ToDisplayString()));

            }

        var returnTypeName = asyncMethodInfo.ReturnType.GetAnnotatedTypeName(model);
        var actionClassName = $"{asyncMethodInfo.ParentClass.Identifier.Text}{method.Identifier.Text}Action";
        var actionBaseClassName = cancellationTokenParameter != null ? "UCSCancellableAsyncAction" : "UCSBlueprintAsyncActionBase";
        var delegateName = $"{actionClassName}Delegate";
        var taskTypeName = asyncMethodInfo.ReturnType != null ? $"Task<{returnTypeName}>" : "Task";
        var paramNameList = string.Join(", ", method.ParameterList.Parameters.Select(p => p == cancellationTokenParameter ? "cancellationToken" : p.Identifier.Text));
        var paramDeclListNoCancellationToken = string.Join(", ", method.ParameterList.Parameters.Where(p => p != cancellationTokenParameter));

        var metadataAttributeList = string.Join(", ", asyncMethodInfo.Metadata.Select(static pair => $"UMetaData({pair.Key}, {pair.Value})"));
        if (string.IsNullOrEmpty(metadataAttributeList))
        {
            metadataAttributeList = "UMetaData(\"BlueprintInternalUseOnly\", \"true\")";
        }
        else
        {
            metadataAttributeList = $"UMetaData(\"BlueprintInternalUseOnly\", \"true\"), {metadataAttributeList}";
        }

        var isStatic = method.Modifiers.Any(static x => x.IsKind(SyntaxKind.StaticKeyword));
        if (!isStatic)
        {
            metadataAttributeList = $"UMetaData(\"DefaultToSelf\", \"Target\"), {metadataAttributeList}";
        }

        var sourceBuilder = new StringBuilder();
        var nullableAnnotation = "?";
        var nullableSuppression = "!";
        if (asyncMethodInfo.NullableAwareable)
        {
            sourceBuilder.AppendLine("#nullable enable");
        }
        else
        {
            sourceBuilder.AppendLine("#nullable disable");
            nullableAnnotation = "";
            nullableSuppression = "";
        }
        sourceBuilder.AppendLine();
        foreach (var ns in namespaces)
        {
            if (!string.IsNullOrWhiteSpace(ns))
            {
                sourceBuilder.AppendLine($"using {ns};");
            }
        }
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"namespace {asyncMethodInfo.Namespace};");
        sourceBuilder.AppendLine();
        if (asyncMethodInfo.ReturnType != null)
        {
            sourceBuilder.AppendLine($"public delegate void {delegateName}({returnTypeName} Result, string{nullableAnnotation} Exception);");
        }
        else
        {
            sourceBuilder.AppendLine($"public delegate void {delegateName}(string{nullableAnnotation} Exception);");
        }
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"public class U{delegateName} : MulticastDelegate<{delegateName}>");
        sourceBuilder.AppendLine("{");
        if (asyncMethodInfo.ReturnType != null)
        {
            sourceBuilder.AppendLine($"    protected void Invoker({returnTypeName} Result, string{nullableAnnotation} Exception)");
            sourceBuilder.AppendLine("    {");
            sourceBuilder.AppendLine("        ProcessDelegate(IntPtr.Zero);");
            sourceBuilder.AppendLine("    }");
        }
        else
        {
            sourceBuilder.AppendLine($"    protected void Invoker(string{nullableAnnotation} Exception)");
            sourceBuilder.AppendLine("    {");
            sourceBuilder.AppendLine("        ProcessDelegate(IntPtr.Zero);");
            sourceBuilder.AppendLine("    }");
        }
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"    protected override {delegateName} GetInvoker()");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine("        return Invoker;");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine("}");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("[UClass]");
        sourceBuilder.AppendLine($"public class {actionClassName} : {actionBaseClassName}");
        sourceBuilder.AppendLine("{");
        sourceBuilder.AppendLine($"    private {taskTypeName}{nullableAnnotation} task;");
        if (cancellationTokenParameter != null)
        {
            sourceBuilder.AppendLine("    private readonly CancellationTokenSource cancellationTokenSource = new();");
            sourceBuilder.AppendLine($"    private Func<CancellationToken, {taskTypeName}>{nullableAnnotation} asyncDelegate;");
        }
        else
        {
            sourceBuilder.AppendLine($"    private Func<{taskTypeName}>{nullableAnnotation} asyncDelegate;");
        }
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"    [UProperty(PropertyFlags.BlueprintAssignable)]");
        sourceBuilder.AppendLine($"    public TMulticastDelegate<{delegateName}>{nullableAnnotation} Completed {{ get; set; }}");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"    [UProperty(PropertyFlags.BlueprintAssignable)]");
        sourceBuilder.AppendLine($"    public TMulticastDelegate<{delegateName}>{nullableAnnotation} Failed {{ get; set; }}");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"    [UFunction(FunctionFlags.BlueprintCallable), {metadataAttributeList}]");
        string conversion = asyncMethodInfo.ReturnsValueTask ? ".AsTask()" : "";
        if (isStatic)
        {
            sourceBuilder.AppendLine($"    public static {actionClassName} {method.Identifier.Text}({paramDeclListNoCancellationToken})");
            sourceBuilder.AppendLine($"    {{");
            sourceBuilder.AppendLine($"        var action = NewObject<{actionClassName}>(GetTransientPackage());");
            
            if (cancellationTokenParameter != null)
            {
                sourceBuilder.AppendLine($"        action.asyncDelegate = (cancellationToken) => {asyncMethodInfo.ParentClass.Identifier.Text}.{method.Identifier.Text}({paramNameList}){conversion};");
            }
            else
            {
                sourceBuilder.AppendLine($"        action.asyncDelegate = () => {asyncMethodInfo.ParentClass.Identifier.Text}.{method.Identifier.Text}({paramNameList}){conversion};");
            }
            sourceBuilder.AppendLine($"        return action;");
            sourceBuilder.AppendLine($"    }}");
        }
        else
        {
            if (string.IsNullOrEmpty(paramDeclListNoCancellationToken))
            {
                sourceBuilder.AppendLine($"    public static {actionClassName} {method.Identifier.Text}({asyncMethodInfo.ParentClass.Identifier.Text} Target)");
            }
            else
            {
                sourceBuilder.AppendLine($"    public static {actionClassName} {method.Identifier.Text}({asyncMethodInfo.ParentClass.Identifier.Text} Target, {paramDeclListNoCancellationToken})");
            }
            sourceBuilder.AppendLine($"    {{");
            sourceBuilder.AppendLine($"        var action = NewObject<{actionClassName}>(Target);");
            if (cancellationTokenParameter != null)
            {
                sourceBuilder.AppendLine($"        action.asyncDelegate = (cancellationToken) => Target.{method.Identifier.Text}({paramNameList}){conversion};");
            }
            else
            {
                sourceBuilder.AppendLine($"        action.asyncDelegate = () => Target.{method.Identifier.Text}({paramNameList}){conversion};");
            }
            sourceBuilder.AppendLine($"        return action;");
            sourceBuilder.AppendLine($"    }}");
        }
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"    protected override void Activate()");
        sourceBuilder.AppendLine($"    {{");
        sourceBuilder.AppendLine($"        if (asyncDelegate == null) {{ throw new InvalidOperationException(\"AsyncDelegate was null\"); }}");
        if (cancellationTokenParameter != null)
        {
            sourceBuilder.AppendLine($"        task = asyncDelegate(cancellationTokenSource.Token);");
        }
        else
        {
            sourceBuilder.AppendLine($"        task = asyncDelegate();");
        }
        sourceBuilder.AppendLine($"        task.ContinueWith(OnTaskCompleted);");
        sourceBuilder.AppendLine($"    }}");
        if (cancellationTokenParameter != null)
        {
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($"    protected override void Cancel()");
            sourceBuilder.AppendLine($"    {{");
            sourceBuilder.AppendLine($"        cancellationTokenSource.Cancel();");
            sourceBuilder.AppendLine($"        base.Cancel();");
            sourceBuilder.AppendLine($"    }}");
        }
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"    private void OnTaskCompleted({taskTypeName} t)");
        sourceBuilder.AppendLine($"    {{");
        // sourceBuilder.AppendLine($"        if (!IsDestroyed) {{ PrintString($\"OnTaskCompleted for {{this}} on {{UnrealSynchronizationContext.CurrentThread}}\"); }}");
        sourceBuilder.AppendLine($"        if (UnrealSynchronizationContext.CurrentThread != NamedThread.GameThread)");
        sourceBuilder.AppendLine($"        {{");
        sourceBuilder.AppendLine($"            new UnrealSynchronizationContext(NamedThread.GameThread, t).Post(_ => OnTaskCompleted(t), null);");
        sourceBuilder.AppendLine($"            return;");
        sourceBuilder.AppendLine($"        }}");
        if (cancellationTokenParameter != null)
        {
            sourceBuilder.AppendLine($"        if (cancellationTokenSource.IsCancellationRequested || IsDestroyed) {{ return; }}");
        }
        else
        {
            sourceBuilder.AppendLine($"        if (IsDestroyed) {{ return; }}");
        }
        sourceBuilder.AppendLine($"        if (t.IsFaulted)");
        sourceBuilder.AppendLine($"        {{");
        if (asyncMethodInfo.ReturnType != null)
        {
            sourceBuilder.AppendLine($"            Failed?.InnerDelegate.Invoke(default{nullableSuppression}, t.Exception?.ToString() ?? \"Faulted without exception\");");
        }
        else
        {
            sourceBuilder.AppendLine($"            Failed?.InnerDelegate.Invoke(t.Exception?.ToString() ?? \"Faulted without exception\");");
        }
        sourceBuilder.AppendLine($"        }}");
        sourceBuilder.AppendLine($"        else");
        sourceBuilder.AppendLine($"        {{");
        if (asyncMethodInfo.ReturnType != null)
        {
            sourceBuilder.AppendLine($"            Completed?.InnerDelegate.Invoke(t.Result, null);");
        }
        else
        {
            sourceBuilder.AppendLine($"            Completed?.InnerDelegate.Invoke(null);");
        }
        sourceBuilder.AppendLine($"        }}");
        sourceBuilder.AppendLine($"    }}");
        sourceBuilder.AppendLine($"}}");

        return sourceBuilder.ToString();
    }

    private static AsyncMethodInfo? GetAsyncMethodInfo(GeneratorSyntaxContext context)
    {
        if (context.Node is not MethodDeclarationSyntax methodDeclaration)
        {
            return null;
        }
        if (methodDeclaration.Parent is not ClassDeclarationSyntax classDeclaration)
        {
            return null;
        }

        var hasUFunctionAttribute = methodDeclaration.AttributeLists
            .SelectMany(a => a.Attributes)
            .Any(a => a.Name.ToString() == "UFunction");
        if (!hasUFunctionAttribute)
        {
            return null;
        }

        TypeSyntax? returnType;
        bool returnsValueTask;
        switch (methodDeclaration.ReturnType)
        {
            case IdentifierNameSyntax { Identifier.ValueText: "Task" }:
                // Method returns non-generic task, e.g. without return value
                returnType = null;
                returnsValueTask = false;
                break;
            case GenericNameSyntax { Identifier.ValueText: "Task" } genericTask:
                // Method returns generic task, e.g. with return value
                returnType = genericTask.TypeArgumentList.Arguments.Single();
                returnsValueTask = false;
                break;
            case IdentifierNameSyntax { Identifier.ValueText: "ValueTask" }:
                // Method returns non-generic task, e.g. without return value
                returnType = null;
                returnsValueTask = true;
                break;
            case GenericNameSyntax { Identifier.ValueText: "ValueTask" } genericValueTask:
                // Method returns generic task, e.g. with return value
                returnType = genericValueTask.TypeArgumentList.Arguments.Single();
                returnsValueTask = true;
                break;
            default:
                return null;
        }

        string namespaceName = methodDeclaration.GetFullNamespace();
        if (string.IsNullOrEmpty(namespaceName))
        {
            return null;
        }

        var metadataAttributes = methodDeclaration.AttributeLists
            .SelectMany(a => a.Attributes)
            .Where(a => a.Name.ToString() == "UMetaData");

        Dictionary<string, string> metadata = new();
        foreach (var metadataAttribute in metadataAttributes)
        {
            if (metadataAttribute.ArgumentList == null || metadataAttribute.ArgumentList.Arguments.Count == 0)
            {
                continue;
            }
            var key = metadataAttribute.ArgumentList.Arguments[0].Expression.ToString();
            var value = metadataAttribute.ArgumentList.Arguments.Count > 1 ? metadataAttribute.ArgumentList.Arguments[1].Expression.ToString() : "";
            metadata[key] = value;
        }

        return new AsyncMethodInfo(classDeclaration, methodDeclaration, namespaceName, returnType, metadata, 
            context.SemanticModel
                .GetNullableContext(context.Node.Span.Start)
                .HasFlag(NullableContext.AnnotationsEnabled), returnsValueTask);
    }
}
