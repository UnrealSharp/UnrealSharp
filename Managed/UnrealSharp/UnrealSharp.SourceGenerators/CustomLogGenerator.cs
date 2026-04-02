using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace UnrealSharp.SourceGenerators;

[Generator]
public class CustomLogSourceGenerator : IIncrementalGenerator
{
    readonly record struct ClassLogInfo
    {
        public readonly string Name;
        public readonly string Namespace;
        public readonly string LogVerbosity;
        
        public ClassLogInfo(ISymbol classSymbol, string logVerbosity)
        {
            Name = classSymbol.Name;
            Namespace = classSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : classSymbol.ContainingNamespace.ToDisplayString();
            LogVerbosity = logVerbosity;
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ClassLogInfo> discoveryResults = context.SyntaxProvider.ForAttributeWithMetadataName("UnrealSharp.Log.CustomLog", Predicate, Transform);
        context.RegisterSourceOutput(discoveryResults, GenerateSource);
    }

    private void GenerateSource(SourceProductionContext sourceProductionContext, ClassLogInfo classLogInfo)
    {
        StringBuilder builder = new StringBuilder();

        builder.AppendLine("using UnrealSharp.Log;");

        if (!string.IsNullOrEmpty(classLogInfo.Namespace))
        {
            builder.AppendLine($"namespace {classLogInfo.Namespace};");
        }

        builder.AppendLine($"public partial class {classLogInfo.Name}");
        builder.AppendLine("{");
        builder.AppendLine($"    public static void Log(string message) => UnrealLogger.Log(\"{classLogInfo.Name}\", message, (ELogVerbosity){classLogInfo.LogVerbosity});");
        builder.AppendLine($"    public static void LogWarning(string message) => UnrealLogger.LogWarning(\"{classLogInfo.Name}\", message);");
        builder.AppendLine($"    public static void LogError(string message) => UnrealLogger.LogError(\"{classLogInfo.Name}\", message);");
        builder.AppendLine($"    public static void LogFatal(string message) => UnrealLogger.LogFatal(\"{classLogInfo.Name}\", message);");
        builder.AppendLine("}");

        sourceProductionContext.AddSource($"{classLogInfo.Name}_CustomLog.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    private ClassLogInfo Transform(GeneratorAttributeSyntaxContext arg1, CancellationToken arg2)
    {
        const string logVerbosityDisplay = "ELogVerbosity.Display";
        AttributeData attribute = arg1.Attributes.First();
        TypedConstant firstArgument = attribute.ConstructorArguments.FirstOrDefault();
        string logVerbosity = firstArgument.IsNull ? logVerbosityDisplay : firstArgument.Value?.ToString() ?? logVerbosityDisplay;
        return new ClassLogInfo(arg1.TargetSymbol, logVerbosity);
    }

    private static bool Predicate(SyntaxNode arg1, CancellationToken arg2) => true;
}