﻿using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnrealSharp.GlueGenerator.NativeTypes;

namespace UnrealSharp.GlueGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class UnrealTypeDiscoveryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        List<InspectorData> inspectors = InspectorManager.GetScopedInspectorData("Global");
        SyntaxValueProvider provider = context.SyntaxProvider;
        
        foreach (InspectorData globalType in inspectors)
        {
            IncrementalValuesProvider<UnrealType> newTypes = provider.ForAttributeWithMetadataName<UnrealType>(
                globalType.InspectAttribute.FullyQualifiedAttributeName, static (_, _) => true,
                static (ctx, _) =>
                {
                    InspectorData decode = InspectorManager.GetInspectorData(ctx.Attributes[0].AttributeClass!.Name)!;
                    UnrealType type = decode.InspectAttributeDelegate!(null, ctx, (MemberDeclarationSyntax) ctx.TargetNode,
                        ctx.Attributes)!;
                    return type;
                });
            
            context.RegisterSourceOutput(newTypes, static (spc, unrealType) => EmitType(spc, unrealType));
        }
    }
    
    private static void EmitType(SourceProductionContext spc, UnrealType utype)
    {
        try 
        {
            GeneratorStringBuilder builder = new GeneratorStringBuilder();
            builder.BeginGeneratedSourceFile(utype);
            
            builder.AppendLine();
            utype.ExportType(builder, spc);
            builder.BeginModuleInitializer(utype);
            
            spc.AddSource(utype.SourceName + ".g.cs", builder.ToString());
        }
        catch (Exception exception)
        {
            DiagnosticDescriptor descriptor = new DiagnosticDescriptor("UTDG001", "UnrealTypeDiscoveryGenerator Error", exception.ToString(), "UnrealTypeDiscoveryGenerator", DiagnosticSeverity.Error, true);
            spc.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None));
        }
    }
}