using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnrealSharp.SourceGenerators;

/// <summary>
/// Reports <c>bool</c> and <c>char</c> in native interop signatures (USSG001): unmanaged function pointer fields,
/// <c>[UnmanagedFunctionPointer]</c> delegates, and the fields of structs they pass by value or by reference.
/// With runtime marshalling enabled, <c>bool</c> is marshalled as a 4-byte Win32 BOOL and <c>char</c> as a 1-byte
/// ANSI char, which match neither C++ <c>bool</c> nor <c>TCHAR</c>. Use <c>NativeBool</c> and <c>char*</c> instead.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NativeInteropSignatureAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor NonBlittableTypeRule = new(
        id: "USSG001",
        title: "Non-blittable type in a native interop signature",
        messageFormat: "'{0}' in the native signature of '{1}' is marshalled as a 4-byte BOOL or 1-byte ANSI char; use NativeBool or char* instead",
        category: "UnrealSharp.SourceGenerators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(NonBlittableTypeRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        context.RegisterSymbolAction(AnalyzeDelegate, SymbolKind.NamedType);
    }

    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        IFieldSymbol field = (IFieldSymbol) context.Symbol;

        if (field.Type is IFunctionPointerTypeSymbol functionPointer
            && functionPointer.Signature.CallingConvention != SignatureCallingConvention.Default)
        {
            Report(context, functionPointer.Signature, field);
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
            Report(context, type.DelegateInvokeMethod, type);
        }
    }

    private static void Report(SymbolAnalysisContext context, IMethodSymbol signature, ISymbol owner)
    {
        IEnumerable<ITypeSymbol> types = signature.Parameters.Select(parameter => parameter.Type).Append(signature.ReturnType);

        foreach (ITypeSymbol type in types)
        {
            ITypeSymbol? offending = FindNonBlittable(type, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default));
            if (offending == null)
            {
                continue;
            }

            Location location = owner.Locations.FirstOrDefault() ?? Location.None;
            context.ReportDiagnostic(Diagnostic.Create(NonBlittableTypeRule, location, offending.ToDisplayString(), owner.ToDisplayString()));
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
