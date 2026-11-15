using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace UnrealSharp.SourceGenerators;

/// <summary>
/// Reports <c>bool</c> and <c>char</c> in native interop signatures (US0015): unmanaged function pointer fields, locals
/// and casts, <c>[UnmanagedFunctionPointer]</c> delegates, and the fields of structs they pass by value or by reference.
/// With runtime marshalling enabled, <c>bool</c> is marshalled as a 4-byte Win32 BOOL and <c>char</c> as a 1-byte
/// ANSI char, which match neither C++ <c>bool</c> nor <c>TCHAR</c>. Use <c>NativeBool</c> and <c>char*</c> instead.
/// Only reported in UnrealSharp assemblies and inside <c>[NativeCallbacks]</c> types, so game code calling other
/// native libraries is unaffected. A <c>[MarshalAs]</c> on the parameter, return value or struct field is an explicit
/// choice and is not reported.
/// <c>string</c> is not reported: it is marshalled as UTF-8 on Unix and the ANSI code page on Windows, which the native
/// binds taking <c>const char*</c> expect.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NativeInteropSignatureAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor NonBlittableTypeRule = new(
        id: "US0015",
        title: "Non-blittable type in a native interop signature",
        messageFormat: "'{0}' in the native signature of '{1}' is marshalled as a 4-byte BOOL or 1-byte ANSI char; use NativeBool or char* instead",
        category: "UnrealSharp",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(NonBlittableTypeRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        string? assemblyName = context.Compilation.AssemblyName;
        bool isUnrealSharp = assemblyName == "UnrealSharp" || (assemblyName != null && assemblyName.StartsWith("UnrealSharp.", StringComparison.Ordinal));
        INamedTypeSymbol? nativeCallbacks = context.Compilation.GetTypeByMetadataName("UnrealSharp.Binds.NativeCallbacksAttribute");

        bool InScope(ISymbol? symbol)
        {
            if (isUnrealSharp)
            {
                return true;
            }

            for (INamedTypeSymbol? type = symbol as INamedTypeSymbol ?? symbol?.ContainingType; type != null; type = type.ContainingType)
            {
                foreach (AttributeData attribute in type.GetAttributes())
                {
                    bool isNativeCallbacks = nativeCallbacks != null
                        ? SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, nativeCallbacks)
                        : attribute.AttributeClass?.Name == "NativeCallbacksAttribute";

                    if (isNativeCallbacks)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        context.RegisterSymbolAction(symbolContext => AnalyzeField(symbolContext, InScope), SymbolKind.Field);
        context.RegisterSymbolAction(symbolContext => AnalyzeDelegate(symbolContext, InScope), SymbolKind.NamedType);
        context.RegisterOperationAction(operationContext => AnalyzeConversion(operationContext, InScope), OperationKind.Conversion);
        context.RegisterOperationAction(operationContext => AnalyzeLocal(operationContext, InScope), OperationKind.VariableDeclarator);
    }

    private static void AnalyzeField(SymbolAnalysisContext context, Func<ISymbol?, bool> inScope)
    {
        IFieldSymbol field = (IFieldSymbol) context.Symbol;

        if (field.Type is IFunctionPointerTypeSymbol functionPointer
            && functionPointer.Signature.CallingConvention != SignatureCallingConvention.Default
            && inScope(field))
        {
            Report(context.ReportDiagnostic, functionPointer.Signature, field, field.Locations.FirstOrDefault() ?? Location.None);
        }
    }

    private static void AnalyzeConversion(OperationAnalysisContext context, Func<ISymbol?, bool> inScope)
    {
        if (context.Operation.Type is IFunctionPointerTypeSymbol functionPointer
            && functionPointer.Signature.CallingConvention != SignatureCallingConvention.Default
            && !IsInitializerOfSameType(context.Operation)
            && inScope(context.ContainingSymbol))
        {
            Report(context.ReportDiagnostic, functionPointer.Signature, context.ContainingSymbol, context.Operation.Syntax.GetLocation());
        }
    }

    // The declarator or field is reported on its own; the cast would repeat it.
    private static bool IsInitializerOfSameType(IOperation conversion)
    {
        if (conversion.Parent is not ISymbolInitializerOperation initializer || initializer.Value != conversion)
        {
            return false;
        }

        ITypeSymbol? declaredType = initializer switch
        {
            IFieldInitializerOperation field => field.InitializedFields.FirstOrDefault()?.Type,
            _ => (initializer.Parent as IVariableDeclaratorOperation)?.Symbol.Type,
        };

        return SymbolEqualityComparer.Default.Equals(declaredType, conversion.Type);
    }

    private static void AnalyzeLocal(OperationAnalysisContext context, Func<ISymbol?, bool> inScope)
    {
        IVariableDeclaratorOperation declarator = (IVariableDeclaratorOperation) context.Operation;

        if (declarator.Symbol.Type is IFunctionPointerTypeSymbol functionPointer
            && functionPointer.Signature.CallingConvention != SignatureCallingConvention.Default
            && inScope(context.ContainingSymbol))
        {
            Report(context.ReportDiagnostic, functionPointer.Signature, declarator.Symbol, declarator.Syntax.GetLocation());
        }
    }

    private static void AnalyzeDelegate(SymbolAnalysisContext context, Func<ISymbol?, bool> inScope)
    {
        INamedTypeSymbol type = (INamedTypeSymbol) context.Symbol;

        if (type.TypeKind != TypeKind.Delegate || type.DelegateInvokeMethod == null)
        {
            return;
        }

        bool isUnmanaged = type.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == "UnmanagedFunctionPointerAttribute");
        if (isUnmanaged && inScope(type))
        {
            Report(context.ReportDiagnostic, type.DelegateInvokeMethod, type, type.Locations.FirstOrDefault() ?? Location.None);
        }
    }

    private static void Report(Action<Diagnostic> reportDiagnostic, IMethodSymbol signature, ISymbol owner, Location location)
    {
        IEnumerable<ITypeSymbol> types = signature.Parameters
            .Where(parameter => !HasMarshalAs(parameter.GetAttributes()))
            .Select(parameter => parameter.Type);

        if (!HasMarshalAs(signature.GetReturnTypeAttributes()))
        {
            types = types.Append(signature.ReturnType);
        }

        foreach (ITypeSymbol type in types)
        {
            ITypeSymbol? offending = FindNonBlittable(type, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default));
            if (offending == null)
            {
                continue;
            }

            reportDiagnostic(Diagnostic.Create(NonBlittableTypeRule, location, offending.ToDisplayString(), owner.ToDisplayString()));
        }
    }

    private static bool HasMarshalAs(ImmutableArray<AttributeData> attributes)
    {
        return attributes.Any(attribute => attribute.AttributeClass?.Name == "MarshalAsAttribute");
    }

    private static ITypeSymbol? FindNonBlittable(ITypeSymbol type, HashSet<ITypeSymbol> visited)
    {
        if (type.SpecialType is SpecialType.System_Boolean or SpecialType.System_Char)
        {
            return type;
        }

        if (type.TypeKind != TypeKind.Struct || type.SpecialType != SpecialType.None || !visited.Add(type))
        {
            return null;
        }

        foreach (IFieldSymbol field in type.GetMembers().OfType<IFieldSymbol>())
        {
            if (field.IsStatic || field.IsConst || HasMarshalAs(field.GetAttributes()))
            {
                continue;
            }

            ITypeSymbol? offending = FindNonBlittable(field.Type, visited);
            if (offending != null)
            {
                return offending;
            }
        }

        return null;
    }
}
