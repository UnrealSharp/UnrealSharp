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
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        context.RegisterSymbolAction(AnalyzeDelegate, SymbolKind.NamedType);
        context.RegisterOperationAction(AnalyzeConversion, OperationKind.Conversion);
        context.RegisterOperationAction(AnalyzeLocal, OperationKind.VariableDeclarator);
    }

    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        IFieldSymbol field = (IFieldSymbol) context.Symbol;

        if (field.Type is IFunctionPointerTypeSymbol functionPointer
            && functionPointer.Signature.CallingConvention != SignatureCallingConvention.Default)
        {
            Report(context.ReportDiagnostic, functionPointer.Signature, field, field.Locations.FirstOrDefault() ?? Location.None);
        }
    }

    private static void AnalyzeConversion(OperationAnalysisContext context)
    {
        if (context.Operation.Type is IFunctionPointerTypeSymbol functionPointer
            && functionPointer.Signature.CallingConvention != SignatureCallingConvention.Default)
        {
            Report(context.ReportDiagnostic, functionPointer.Signature, context.ContainingSymbol, context.Operation.Syntax.GetLocation());
        }
    }

    private static void AnalyzeLocal(OperationAnalysisContext context)
    {
        IVariableDeclaratorOperation declarator = (IVariableDeclaratorOperation) context.Operation;

        if (declarator.Symbol.Type is IFunctionPointerTypeSymbol functionPointer
            && functionPointer.Signature.CallingConvention != SignatureCallingConvention.Default)
        {
            Report(context.ReportDiagnostic, functionPointer.Signature, declarator.Symbol, declarator.Syntax.GetLocation());
        }
    }

    private static void AnalyzeDelegate(SymbolAnalysisContext context)
    {
        INamedTypeSymbol type = (INamedTypeSymbol) context.Symbol;

        if (type.TypeKind != TypeKind.Delegate || type.DelegateInvokeMethod == null)
        {
            return;
        }

        bool isUnmanaged = type.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == "UnmanagedFunctionPointerAttribute");
        if (isUnmanaged)
        {
            Report(context.ReportDiagnostic, type.DelegateInvokeMethod, type, type.Locations.FirstOrDefault() ?? Location.None);
        }
    }

    private static void Report(System.Action<Diagnostic> reportDiagnostic, IMethodSymbol signature, ISymbol owner, Location location)
    {
        IEnumerable<ITypeSymbol> types = signature.Parameters.Select(parameter => parameter.Type).Append(signature.ReturnType);

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
            if (field.IsStatic || field.IsConst)
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
