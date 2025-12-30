using System.Linq;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public record UnrealFunction : UnrealFunctionBase
{
    private bool _isImplementationMethodVirtual;
    
    public UnrealFunction(IMethodSymbol typeSymbol, UnrealType outer) : base(typeSymbol, outer)
    {
    }

    public UnrealFunction(EFunctionFlags flags, string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName, UnrealType? outer = null) : base(flags, sourceName, typeNameSpace, accessibility, assemblyName, outer)
    {
    }

    public override void PostParse(ISymbol symbol)
    {
        base.PostParse(symbol);

        if (IsEvent)
        {
            ISymbol? foundSymbol = symbol.ContainingType.GetMemberSymbolByName($"{SourceName}_Implementation");
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
            string instanceFunctionName;
            if (IsEvent)
            {
                instanceFunctionName = $"{SourceName}_InstanceFunction";
                
                builder.AppendLine();
                builder.AppendEditorBrowsableAttribute();
                builder.AppendLine($"IntPtr {instanceFunctionName} = IntPtr.Zero;");
            }
            else
            {
                instanceFunctionName = FunctionNativePtr;
            }
            
            ExportWrapperMethod(builder, instanceFunctionName);
            ExportImplementationMethod(builder);
        }
    }
    
    public void ExportImplementationMethod(GeneratorStringBuilder builder)
    {
        string virtualModifier = _isImplementationMethodVirtual ? "virtual " : string.Empty;
        builder.AppendLine($"{TypeAccessibility.AccessibilityToString()}{virtualModifier}partial {ReturnType.ManagedType} {SourceName}_Implementation({string.Join(", ", Properties.Select(p => $"{p.ManagedType} {p.SourceName}"))});");
    }

    public void ExportWrapperMethod(GeneratorStringBuilder builder, string instanceFunction)
    {
        builder.AppendLine();
        
        builder.AppendLine($"{TypeAccessibility.AccessibilityToString()}partial {ReturnType.ManagedType} {SourceName}({string.Join(", ", Properties.Select(p => $"{p.ManagedType} {p.SourceName}"))})");
        builder.OpenBrace();

        if (FunctionFlags.HasFlag(EFunctionFlags.Event))
        {
            builder.AppendLine($"if ({instanceFunction} == IntPtr.Zero)");
            builder.OpenBrace();
            builder.AppendLine($"{instanceFunction} = CallGetNativeFunctionFromInstanceAndName(NativeObject, \"{SourceName}\");");
            builder.CloseBrace();
        }
        
        if (HasParamsOrReturnValue)
        {
            ExportCallToNative(builder, (paramsBuffer, returnBuffer) =>
            {
                AppendCallInvokeNativeFunction(builder, instanceFunction, paramsBuffer, returnBuffer);
            });
        }
        else
        {
            AppendCallInvokeNativeFunction(builder, instanceFunction, SourceGenUtilities.IntPtrZero, SourceGenUtilities.IntPtrZero);
        }
        
        builder.CloseBrace();
    }
}