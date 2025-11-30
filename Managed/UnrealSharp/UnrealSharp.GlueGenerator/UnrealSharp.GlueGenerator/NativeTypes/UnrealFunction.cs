using Microsoft.CodeAnalysis;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public record UnrealFunction : UnrealFunctionBase
{
    public UnrealFunction(IMethodSymbol typeSymbol, UnrealType outer) : base(typeSymbol, outer)
    {
    }

    public UnrealFunction(EFunctionFlags flags, string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName, UnrealType? outer = null) : base(flags, sourceName, typeNameSpace, accessibility, assemblyName, outer)
    {
    }


    public override void ExportBackingVariables(GeneratorStringBuilder builder)
    {
        base.ExportBackingVariables(builder);
        builder.AppendNewBackingField($"static IntPtr {FunctionNativePtr};");
    }

    public override void ExportBackingVariablesToStaticConstructor(GeneratorStringBuilder builder, string nativeType)
    {
        builder.AppendLine($"{FunctionNativePtr} = CallGetNativeFunctionFromClassAndName({SourceGenUtilities.NativeTypePtr}, \"{SourceName}\");");
        base.ExportBackingVariablesToStaticConstructor(builder, nativeType);
    }

    public override void ExportType(GeneratorStringBuilder builder, SourceProductionContext spc)
    {
        if (HasParamsOrReturnValue || NeedsImplementationFunction)
        {
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