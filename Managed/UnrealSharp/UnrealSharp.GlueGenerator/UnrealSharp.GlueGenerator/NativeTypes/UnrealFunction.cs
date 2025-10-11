using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public record UnrealFunction : UnrealFunctionBase
{
    public UnrealFunction(SemanticModel model, ISymbol typeSymbol, MethodDeclarationSyntax syntax, UnrealType outer) : base(model, typeSymbol, syntax, outer)
    {
    }

    public UnrealFunction(SemanticModel model, ISymbol typeSymbol, DelegateDeclarationSyntax syntax, UnrealType outer) : base(model, typeSymbol, syntax, outer)
    {
    }

    public UnrealFunction(EFunctionFlags flags, string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName, UnrealType? outer = null) : base(flags, sourceName, typeNameSpace, accessibility, assemblyName, outer)
    {
    }

    public override void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc)
    {
        if (HasParamsOrReturnValue || NeedsImplementationFunction)
        {
            builder.AppendNewBackingField($"static IntPtr {FunctionNativePtr} = CallGetNativeFunctionFromClassAndName({SourceGenUtilities.NativeTypePtr}, \"{SourceName}\");");
            ExportBackingVariables(builder);
            builder.AppendLine();
        }

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
}