using System.Linq;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public record UnrealFunction : UnrealFunctionBase
{
    private bool _isImplementationMethodVirtual;

    public UnrealFunction(IMethodSymbol typeSymbol, UnrealType outer) : base(typeSymbol, outer)
    {
    }

    public UnrealFunction(EFunctionFlags flags, string sourceName, string typeNameSpace, Accessibility accessibility,
        string assemblyName, UnrealType? outer = null)
        : base(flags, sourceName, typeNameSpace, accessibility, assemblyName, outer)
    {
    }

    public override void PostParse(ISymbol symbol)
    {
        base.PostParse(symbol);

        if (IsEvent)
        {
            ISymbol? foundSymbol =
                symbol.ContainingType.GetMemberSymbolByName($"{FieldName.SourceName}_Implementation");
            if (foundSymbol is not null && foundSymbol.Kind == SymbolKind.Method && foundSymbol.IsVirtual)
            {
                _isImplementationMethodVirtual = true;
            }
        }
    }

    public override void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc)
    {
        ExportBackingVariables(builder);
        ExportInvokeMethod(builder);

        if (NeedsImplementationFunction)
        {
            ExportWrapperMethod(builder, "partial ");
            ExportImplementationMethod(builder);
        }
    }

    public void ExportImplementationMethod(GeneratorStringBuilder builder)
    {
        string virtualModifier = _isImplementationMethodVirtual ? "virtual " : string.Empty;
        builder.AppendLine(
            $"{Accessibility.AccessibilityToString()}{virtualModifier}partial {ReturnType.ManagedTypeWithNullability} {FieldName.SourceName}_Implementation({string.Join(", ", Properties.Select(p => p.GetParameterDeclaration()))});");
    }

    public void ExportWrapperMethod(GeneratorStringBuilder builder, string modifiers)
    {
        // Interface declarations provide the parameter layout, but the implementation
        // UFunction belongs to the object's class even for non-event functions.
        bool resolveOnInstance = IsEvent || Outer is UnrealInterface;
        string instanceFunctionName;
        if (resolveOnInstance)
        {
            instanceFunctionName = $"{FieldName.SourceName}_InstanceFunction";

            builder.AppendLine();
            builder.AppendEditorBrowsableAttribute();
            builder.AppendLine($"IntPtr {instanceFunctionName} = IntPtr.Zero;");
        }
        else
        {
            instanceFunctionName = FunctionNativePtr;
        }

        builder.AppendLine();

        builder.AppendLine(
            $"{Accessibility.AccessibilityToString()}{modifiers}{ReturnType.ManagedTypeWithNullability} {FieldName.SourceName}({string.Join(", ", Properties.Select(p => p.GetParameterDeclaration()))})");
        builder.OpenBrace();

        if (resolveOnInstance)
        {
            builder.AppendLine($"if ({instanceFunctionName} == IntPtr.Zero)");
            builder.OpenBrace();
            builder.AppendLine(
                $"{instanceFunctionName} = CallGetNativeFunctionFromInstanceAndName(NativeObject, \"{FieldName.SourceName}\");");
            builder.CloseBrace();
        }

        if (HasParamsOrReturnValue)
        {
            ExportCallToNative(builder,
                (paramsBuffer, returnBuffer) =>
                {
                    AppendCallInvokeNativeFunction(builder, instanceFunctionName, paramsBuffer, returnBuffer);
                });
        }
        else
        {
            AppendCallInvokeNativeFunction(builder, instanceFunctionName, SourceGenUtilities.IntPtrZero,
                SourceGenUtilities.IntPtrZero);
        }

        builder.CloseBrace();
    }
}
