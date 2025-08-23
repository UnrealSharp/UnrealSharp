using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UnrealSharp.SourceGenerators;

internal readonly struct AsyncMethodInfo(ClassDeclarationSyntax parentClass, MethodDeclarationSyntax method, string @namespace, TypeSyntax returnType, IReadOnlyDictionary<string, string> metadata)
{
    public readonly ClassDeclarationSyntax ParentClass = parentClass;
    public readonly MethodDeclarationSyntax Method = method;
    public readonly string Namespace = @namespace;
    public readonly TypeSyntax ReturnType = returnType;
    public readonly IReadOnlyDictionary<string, string> Metadata = metadata;
}

[Generator]
public class AsyncWrapperGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a syntax receiver that will be created for each compilation
        context.RegisterForSyntaxNotifications(() => new AsyncSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not AsyncSyntaxReceiver receiver)
        {
            return;
        }

        var compilation = context.Compilation;

        var cancellationTokenType = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

        foreach (var asyncMethodInfo in receiver.AsyncMethods)
        {
            var model = compilation.GetSemanticModel(asyncMethodInfo.Method.SyntaxTree);
            var sourceBuilder = new StringBuilder();

            HashSet<string> namespaces = ["UnrealSharp", "UnrealSharp.Attributes", "UnrealSharp.UnrealSharpCore"];

            ParameterSyntax cancellationTokenParameter = null;
            foreach (var parameter in asyncMethodInfo.Method.ParameterList.Parameters)
            {
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

            sourceBuilder.AppendLine("#nullable disable");
            sourceBuilder.AppendLine();

            foreach (var ns in namespaces)
            {
                sourceBuilder.AppendLine($"using {ns};");
            }

            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($"namespace {asyncMethodInfo.Namespace};");

            var isStatic = asyncMethodInfo.Method.Modifiers.Any(static x => x.IsKind(SyntaxKind.StaticKeyword));

            var returnTypeName = asyncMethodInfo.ReturnType != null ? model.GetTypeInfo(asyncMethodInfo.ReturnType).Type.Name : null;
            var actionClassName = $"{asyncMethodInfo.ParentClass.Identifier.Text}{asyncMethodInfo.Method.Identifier.Text}Action";
            var actionBaseClassName = cancellationTokenParameter != null ? "UCSCancellableAsyncAction" : "UCSBlueprintAsyncActionBase";
            var delegateName = $"{actionClassName}Delegate";
            var taskTypeName = asyncMethodInfo.ReturnType != null ? $"Task<{returnTypeName}>" : "Task";
            var paramNameList = string.Join(", ", asyncMethodInfo.Method.ParameterList.Parameters.Select(p => p == cancellationTokenParameter ? "cancellationToken" : p.Identifier.Text));
            var paramDeclListNoCancellationToken = string.Join(", ", asyncMethodInfo.Method.ParameterList.Parameters.Where(p => p != cancellationTokenParameter));

            var metadataAttributeList = string.Join(", ", asyncMethodInfo.Metadata.Select(static pair => $"UMetaData({pair.Key}, {pair.Value})"));
            if (string.IsNullOrEmpty(metadataAttributeList))
            {
                metadataAttributeList = "UMetaData(\"BlueprintInternalUseOnly\", \"true\")";
            }
            else
            {
                metadataAttributeList = $"UMetaData(\"BlueprintInternalUseOnly\", \"true\"), {metadataAttributeList}";
            }
            if (!isStatic)
            {
                metadataAttributeList = $"UMetaData(\"DefaultToSelf\", \"Target\"), {metadataAttributeList}";
            }

            sourceBuilder.AppendLine();
            
            if (asyncMethodInfo.ReturnType != null)
            {
                sourceBuilder.AppendLine($"public delegate void {delegateName}({returnTypeName} Result, string Exception);");
            }
            else
            {
                sourceBuilder.AppendLine($"public delegate void {delegateName}(string Exception);");
            }

            sourceBuilder.AppendLine();

            sourceBuilder.AppendLine($"public class U{delegateName} : MulticastDelegate<{delegateName}>");
            sourceBuilder.AppendLine($"{{");
            
            if (asyncMethodInfo.ReturnType != null)
            {
                sourceBuilder.AppendLine($"    protected void Invoker({returnTypeName} Result, string Exception)");
                sourceBuilder.AppendLine($"    {{");
                sourceBuilder.AppendLine($"        ProcessDelegate(IntPtr.Zero);");
                sourceBuilder.AppendLine($"    }}");
            }
            else
            {
                sourceBuilder.AppendLine($"    protected void Invoker(string Exception)");
                sourceBuilder.AppendLine($"    {{");
                sourceBuilder.AppendLine($"        ProcessDelegate(IntPtr.Zero);");
                sourceBuilder.AppendLine($"    }}");
            }
            
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($"    protected override {delegateName} GetInvoker()");
            sourceBuilder.AppendLine($"    {{");
            sourceBuilder.AppendLine($"        return Invoker;");
            sourceBuilder.AppendLine($"    }}");
            sourceBuilder.AppendLine($"}}");
            sourceBuilder.AppendLine();
            
            sourceBuilder.AppendLine($"[UClass]");
            sourceBuilder.AppendLine($"public class {actionClassName} : {actionBaseClassName}");
            sourceBuilder.AppendLine($"{{");
            sourceBuilder.AppendLine($"    private {taskTypeName}? task;");
            
            if (cancellationTokenParameter != null)
            {
                sourceBuilder.AppendLine($"    private readonly CancellationTokenSource cancellationTokenSource = new();");
                sourceBuilder.AppendLine($"    private Func<CancellationToken, {taskTypeName}>? asyncDelegate;");
            }
            else
            {
                sourceBuilder.AppendLine($"    private Func<{taskTypeName}>? asyncDelegate;");
            }
            sourceBuilder.AppendLine($"");
            sourceBuilder.AppendLine($"    [UProperty(PropertyFlags.BlueprintAssignable)]");
            sourceBuilder.AppendLine($"    public TMulticastDelegate<{delegateName}> Completed {{ get; set; }}");
            sourceBuilder.AppendLine($"");
            sourceBuilder.AppendLine($"    [UProperty(PropertyFlags.BlueprintAssignable)]");
            sourceBuilder.AppendLine($"    public TMulticastDelegate<{delegateName}> Failed {{ get; set; }}");
            sourceBuilder.AppendLine($"");
            sourceBuilder.AppendLine($"    [UFunction(FunctionFlags.BlueprintCallable), {metadataAttributeList}]");
            if (isStatic)
            {
                sourceBuilder.AppendLine($"    public static {actionClassName} {asyncMethodInfo.Method.Identifier.Text}({paramDeclListNoCancellationToken})");
                sourceBuilder.AppendLine($"    {{");
                sourceBuilder.AppendLine($"        var action = NewObject<{actionClassName}>(GetTransientPackage());");
                if (cancellationTokenParameter != null)
                {
                    sourceBuilder.AppendLine($"        action.asyncDelegate = (cancellationToken) => {asyncMethodInfo.ParentClass.Identifier.Text}.{asyncMethodInfo.Method.Identifier.Text}({paramNameList});");
                }
                else
                {
                    sourceBuilder.AppendLine($"        action.asyncDelegate = () => {asyncMethodInfo.ParentClass.Identifier.Text}.{asyncMethodInfo.Method.Identifier.Text}({paramNameList});");
                }
                sourceBuilder.AppendLine($"        return action;");
                sourceBuilder.AppendLine($"    }}");
            }
            else
            {
                if (string.IsNullOrEmpty(paramDeclListNoCancellationToken))
                {
                    sourceBuilder.AppendLine($"    public static {actionClassName} {asyncMethodInfo.Method.Identifier.Text}({asyncMethodInfo.ParentClass.Identifier.Text} Target)");
                }
                else
                {
                    sourceBuilder.AppendLine($"    public static {actionClassName} {asyncMethodInfo.Method.Identifier.Text}({asyncMethodInfo.ParentClass.Identifier.Text} Target, {paramDeclListNoCancellationToken})");
                }
                sourceBuilder.AppendLine($"    {{");
                sourceBuilder.AppendLine($"        var action = NewObject<{actionClassName}>(Target);");
                if (cancellationTokenParameter != null)
                {
                    sourceBuilder.AppendLine($"        action.asyncDelegate = (cancellationToken) => Target.{asyncMethodInfo.Method.Identifier.Text}({paramNameList});");
                }
                else
                {
                    sourceBuilder.AppendLine($"        action.asyncDelegate = () => Target.{asyncMethodInfo.Method.Identifier.Text}({paramNameList});");
                }
                sourceBuilder.AppendLine($"        return action;");
                sourceBuilder.AppendLine($"    }}");
            }
            

            sourceBuilder.AppendLine($"");
            sourceBuilder.AppendLine($"    protected override void Activate()");
            sourceBuilder.AppendLine($"    {{");
            sourceBuilder.AppendLine($"        if (asyncDelegate == null) {{ throw new InvalidOperationException($\"AsyncDelegate was null\"); }}");
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
                sourceBuilder.AppendLine($"");
                sourceBuilder.AppendLine($"    protected override void Cancel()");
                sourceBuilder.AppendLine($"    {{");
                sourceBuilder.AppendLine($"        cancellationTokenSource.Cancel();");
                sourceBuilder.AppendLine($"        base.Cancel();");
                sourceBuilder.AppendLine($"    }}");
            }
            sourceBuilder.AppendLine($"");
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
                sourceBuilder.AppendLine($"            Failed.InnerDelegate.Invoke(default, t.Exception?.ToString() ?? \"Faulted without exception\");");
            }
            else
            {
                sourceBuilder.AppendLine($"            Failed.InnerDelegate.Invoke(t.Exception?.ToString() ?? \"Faulted without exception\");");
            }
            sourceBuilder.AppendLine($"        }}");
            sourceBuilder.AppendLine($"        else");
            sourceBuilder.AppendLine($"        {{");
            if (asyncMethodInfo.ReturnType != null)
            {
                sourceBuilder.AppendLine($"            Completed.InnerDelegate.Invoke(t.Result, null);");
            }
            else
            {
                sourceBuilder.AppendLine($"            Completed.InnerDelegate.Invoke(null);");
            }
            sourceBuilder.AppendLine($"        }}");
            sourceBuilder.AppendLine($"    }}");
            sourceBuilder.AppendLine($"}}");

            context.AddSource($"{asyncMethodInfo.ParentClass.Identifier.Text}.{asyncMethodInfo.Method.Identifier.Text}.generated.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }
    }
}

internal class AsyncSyntaxReceiver : ISyntaxReceiver
{
    public List<AsyncMethodInfo> AsyncMethods { get; } = [];

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not MethodDeclarationSyntax methodDeclaration)
        {
            return;
        }

        if (methodDeclaration.Parent is not ClassDeclarationSyntax classDeclaration)
        {
            return;
        }
        
        var hasUFunctionAttribute = methodDeclaration.AttributeLists
            .SelectMany(a => a.Attributes)
            .Any(a => a.Name.ToString() == "UFunction");

        if (!hasUFunctionAttribute)
        {
            return;
        }

        string namespaceName = null;
        SyntaxNode currentNode = methodDeclaration.Parent;
        while (currentNode != null)
        {
            // Check if the current node is a NamespaceDeclarationSyntax
            if (currentNode is FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclaration)
            {
                // Get the name of the namespace
                namespaceName = fileScopedNamespaceDeclaration.Name.ToString();
                break;
            }
            if (currentNode is NamespaceDeclarationSyntax namespaceDeclaration)
            {
                // Get the name of the namespace
                namespaceName = namespaceDeclaration.Name.ToString();
                break;
            }

            // Move up to the next parent node
            currentNode = currentNode.Parent;
        }

        if (namespaceName == null)
        {
            return;
        }

        var metadataAttributes = methodDeclaration.AttributeLists
            .SelectMany(a => a.Attributes)
            .Where(a => a.Name.ToString() == "UMetaData");

        Dictionary<string, string> metadata = [];
        foreach (var metadataAttribute in metadataAttributes)
        {
            var key = metadataAttribute.ArgumentList.Arguments[0].Expression.ToString();
            var value = metadataAttribute.ArgumentList.Arguments.Count > 1 ? metadataAttribute.ArgumentList.Arguments[1].Expression.ToString() : "";
            metadata.Add(key, value);
        }

        if (methodDeclaration.ReturnType is IdentifierNameSyntax identifierName && identifierName.Identifier.ValueText == "Task")
        {
            // Method returns non-generic task, e.g. without return value
            AsyncMethods.Add(new(classDeclaration, methodDeclaration, namespaceName, null, metadata));
            return;
        }

        if (methodDeclaration.ReturnType is GenericNameSyntax genericName && genericName.Identifier.ValueText == "Task")
        {
            // Method returns generic task, e.g. with return value
            var taskReturnType = genericName.TypeArgumentList.Arguments.Single();
            AsyncMethods.Add(new(classDeclaration, methodDeclaration, namespaceName, taskReturnType, metadata));
            return;
        }
    }
}
