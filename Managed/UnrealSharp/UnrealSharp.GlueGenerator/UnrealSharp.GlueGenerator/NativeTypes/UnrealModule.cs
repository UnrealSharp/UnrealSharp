using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes;

[Inspector]
public record UnrealModule : UnrealType
{
	public UnrealModule(ISymbol memberSymbol, UnrealType? outer = null, SyntaxNode? syntaxNode = null) : base(memberSymbol, outer, syntaxNode)
	{
	}

	public UnrealModule(string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName, UnrealType? outer = null) : base(sourceName, typeNameSpace, accessibility, assemblyName, outer)
	{
	}

	[Inspect("UnrealSharp.Attributes.UModuleAttribute", "UModuleAttribute", "Global")]
	public static UnrealType UModuleAttribute(UnrealType? outer, SyntaxNode? syntaxNode, GeneratorAttributeSyntaxContext ctx, ISymbol symbol, IReadOnlyList<AttributeData> attributes)
	{
		return new UnrealModule((ITypeSymbol) symbol, outer, syntaxNode);
	}

	public override void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc)
	{
		builder.AppendLine("using UnrealSharp.Engine.Core.Modules;");
		builder.AppendLine("using UnrealSharp.Plugins;");
		
		builder.StartModuleInitializer($"{SourceName}ModuleRegistrar");
		
		builder.AppendLine("public static void Register()");
		builder.OpenBrace();
		builder.AppendLine($"Plugin hostPlugin = PluginLoader.FindPlugin(typeof({SourceName}))!;");
		builder.AppendLine("hostPlugin.AddModuleInterfaceInit(CreateModule);");
		builder.CloseBrace();
		
		builder.AppendLine($"static IModuleInterface CreateModule() => new {SourceName}();");
		
		builder.CloseBrace();
	}
}