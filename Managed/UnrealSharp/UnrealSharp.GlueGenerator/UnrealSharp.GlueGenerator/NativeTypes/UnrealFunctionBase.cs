using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using UnrealSharp.GlueGenerator.NativeTypes.Properties;

namespace UnrealSharp.GlueGenerator.NativeTypes;

[Flags]
public enum EFunctionFlags : ulong
{
    None = 0x00000000,
    Final = 0x00000001,
    RequiredAPI = 0x00000002,
    BlueprintAuthorityOnly = 0x00000004,
    BlueprintCosmetic = 0x00000008,
    Net = 0x00000040,
    NetReliable = 0x00000080,
    NetRequest = 0x00000100,
    Exec = 0x00000200,
    Native = 0x00000400,
    Event = 0x00000800,
    NetResponse = 0x00001000,
    Static = 0x00002000,
    NetMulticast = 0x00004000,
    MulticastDelegate = 0x00010000,
    Public = 0x00020000,
    Private = 0x00040000,
    Protected = 0x00080000,
    Delegate = 0x00100000,
    NetServer = 0x00200000,
    HasOutParms = 0x00400000,
    HasDefaults = 0x00800000,
    NetClient = 0x01000000,
    DLLImport = 0x02000000,
    BlueprintCallable = 0x04000000,
    BlueprintNativeEvent = 0x08000000,
    BlueprintPure = 0x10000000,
    EditorOnly = 0x20000000,
    Const = 0x40000000,
    NetValidate = 0x80000000,
};


[Inspector]
public abstract record UnrealFunctionBase : UnrealStruct
{
    public override string EngineName => SourceName;
    
    public UnrealProperty ReturnType;
    public EFunctionFlags FunctionFlags;

    protected static readonly EFunctionFlags NetFunctionFlags = EFunctionFlags.NetServer | EFunctionFlags.NetClient | EFunctionFlags.NetMulticast;
    protected const string UFunctionAttributeName = "UFunctionAttribute";
    
    protected bool IsNetworkFunction => (FunctionFlags & NetFunctionFlags) != EFunctionFlags.None;

    protected bool NeedsImplementationFunction => IsNetworkFunction || FunctionFlags.HasFlag(EFunctionFlags.BlueprintNativeEvent);
    protected bool IsEvent => FunctionFlags.HasFlag(EFunctionFlags.Event);
    protected bool HasParamsOrReturnValue => HasParams || HasReturnValue;
    protected bool HasParams => Properties.Count > 0;
    protected bool HasReturnValue => ReturnType is not VoidProperty;
    
    public string SizeVariableName => $"{SourceName}_Size";
    public string FunctionNativePtr => $"{SourceName}Ptr";

    public UnrealFunctionBase(IMethodSymbol typeSymbol, UnrealType outer) : this(typeSymbol, typeSymbol.ReturnType, typeSymbol.Parameters, outer!)
    {
        IMethodSymbol methodSymbol = typeSymbol;
        
        FunctionFlags |= methodSymbol.DeclaredAccessibility switch
        {
            Accessibility.Public => EFunctionFlags.Public,
            Accessibility.Private => EFunctionFlags.Private,
            Accessibility.Protected => EFunctionFlags.Protected,
            Accessibility.NotApplicable => EFunctionFlags.Private,
            _ => EFunctionFlags.Public
        };
        
        if (methodSymbol.IsStatic)
        {
            FunctionFlags |= EFunctionFlags.Static;
        }
    }
    
    public UnrealFunctionBase(ISymbol typeSymbol, ITypeSymbol returnType, ImmutableArray<IParameterSymbol> parameterList, UnrealType outer) : base(typeSymbol, outer)
    {
        bool hasOutParams = false;
        if (returnType.Name == VoidProperty.VoidTypeName)
        {
            ReturnType = new VoidProperty(this);
        }
        else
        {
            ReturnType = PropertyFactory.CreateProperty(returnType, returnType, this);
            ReturnType.SourceName = "ReturnValue";
            ReturnType.MakeReturnParameter();
            
            hasOutParams = true;
        }

        if (parameterList.Length <= 0)
        {
            return;
        }
        
        List<UnrealProperty> parameters = new List<UnrealProperty>(parameterList.Length);
        
        for (int i = 0; i < parameterList.Length; i++)
        {
            IParameterSymbol parameterSymbol = parameterList[i];
                
            UnrealProperty property = PropertyFactory.CreateProperty(parameterSymbol.Type, parameterSymbol, this);
            property.ReferenceKind = parameterSymbol.RefKind;
            
            switch (parameterSymbol.RefKind)
            {
                case RefKind.Out:
                    property.MakeOutParameter();
                    hasOutParams = true;
                    break;
                case RefKind.Ref:
                    property.MakeRefParameter();
                    hasOutParams = true;
                    break;
                case RefKind.None:
                    property.MakeParameter();
                    break;
            }
            
            if (parameterSymbol.HasExplicitDefaultValue)
            {
                string defaultValue;
                if (parameterSymbol.Type.TypeKind == TypeKind.Enum)
                {
                    defaultValue = SourceGenUtilities.GetEnumNameFromValue(parameterSymbol.Type, (byte) parameterSymbol.ExplicitDefaultValue!);
                }
                else
                {
                    defaultValue = parameterSymbol.ExplicitDefaultValue!.ToString();
                }
                
                AddMetaData($"CPP_Default_{parameterSymbol.Name}", defaultValue);
                FunctionFlags |= EFunctionFlags.HasDefaults;
            }

            parameters.Add(property);
        }
        
        if (hasOutParams)
        {
            FunctionFlags |= EFunctionFlags.HasOutParms;
        }
            
        Properties = new EquatableList<UnrealProperty>(parameters);
    }
    
    public UnrealFunctionBase(EFunctionFlags flags, string sourceName, string typeNameSpace, Accessibility accessibility, string assemblyName, UnrealType? outer = null) 
        : base(sourceName, typeNameSpace, accessibility, assemblyName, outer)
    {
        FunctionFlags = flags;
    }

    [Inspect("UnrealSharp.Attributes.UFunctionAttribute", "UFunctionAttribute")]
    public static UnrealType? UFunctionAttribute(UnrealType? outer, SyntaxNode? syntaxNode, GeneratorAttributeSyntaxContext ctx, ISymbol symbol, IReadOnlyList<AttributeData> attributes)
    {
        UnrealClassBase unrealClass = (UnrealClassBase) outer!;
        IMethodSymbol methodSymbol = (IMethodSymbol) symbol;

        UnrealFunctionBase unrealFunction;
        if (methodSymbol.IsAsync)
        {
            UnrealAsyncFunction asyncFunction = new UnrealAsyncFunction(methodSymbol, outer!);
            unrealClass.AddAsyncFunction(asyncFunction);
            unrealFunction = asyncFunction;
        }
        else
        {
            unrealFunction = new UnrealFunction(methodSymbol, outer!);
            unrealClass.AddFunction(unrealFunction);
        }

        return unrealFunction;
    }
    
    [InspectArgument(["FunctionFlags", "flags"], UFunctionAttributeName)]
    public static void FlagsSpecifier(UnrealType topScope, TypedConstant constant)
    {
        UnrealFunctionBase unrealFunction = (UnrealFunctionBase) topScope;
        
        unrealFunction.FunctionFlags |= (EFunctionFlags) constant.Value!;
        
        if (unrealFunction.FunctionFlags.HasFlag(EFunctionFlags.BlueprintPure))
        {
            unrealFunction.FunctionFlags |= EFunctionFlags.BlueprintCallable;
        }
        
        if (unrealFunction.FunctionFlags.HasFlag(EFunctionFlags.BlueprintNativeEvent))
        {
            unrealFunction.FunctionFlags |= EFunctionFlags.Event;
        }

        if (unrealFunction.IsNetworkFunction)
        {
            unrealFunction.FunctionFlags |= EFunctionFlags.Net;
        }
    }
    
    [InspectArgument("CallInEditor", UFunctionAttributeName)]
    public static void CallInEditorSpecifier(UnrealType topScope, TypedConstant constant)
    {
        UnrealFunctionBase unrealFunction = (UnrealFunctionBase) topScope;
        bool value = (bool) constant.Value!;
        unrealFunction.AddMetaData("CallInEditor", value ? "true" : "false");
    }
    
    [InspectArgument("Category", UFunctionAttributeName)]
    public static void CategorySpecifier(UnrealType topScope, TypedConstant constant)
    {
        UnrealFunctionBase unrealFunction = (UnrealFunctionBase) topScope;
        unrealFunction.AddMetaData("Category", (string) constant.Value!);
    }

    public override void ExportBackingVariables(GeneratorStringBuilder builder)
    {
        if ((HasParamsOrReturnValue || NeedsImplementationFunction) && !FunctionFlags.HasFlag(EFunctionFlags.Delegate))
        {
            builder.AppendNewBackingField($"static IntPtr {FunctionNativePtr};");
        }
        
        if (HasParamsOrReturnValue)
        {
            builder.AppendNewBackingField($"static int {SizeVariableName};");
        }
            
        if (HasReturnValue)
        {
            ReturnType.ExportBackingVariables(builder);
        }
        
        base.ExportBackingVariables(builder);
    }

    public override void ExportBackingVariablesToStaticConstructor(GeneratorStringBuilder builder, string nativeType)
    {
        bool isDelegate = FunctionFlags.HasFlag(EFunctionFlags.Delegate);
        bool needsFunctionPtr = (HasParamsOrReturnValue || NeedsImplementationFunction) && !isDelegate;

        // Network functions (RPCs) are a special case:
        // - We need a UFunction pointer here to read parameter sizes/offsets.
        // - But we avoid caching a UFunction pointer for invocation during static initialization because in-editor it
        //   may come from a SkeletonGeneratedClass and lack runtime replication metadata.
        //   The wrapper will resolve and cache the correct UFunction lazily from the instance at runtime.
        string functionPtrForOffsets = FunctionNativePtr;
        if (needsFunctionPtr)
        {
            if (IsNetworkFunction)
            {
                if (HasParamsOrReturnValue)
                {
                    functionPtrForOffsets = $"{FunctionNativePtr}_Local";
                    builder.AppendLine($"IntPtr {functionPtrForOffsets} = CallGetNativeFunctionFromClassAndName({nativeType}, \"{SourceName}\");");
                }
            }
            else
            {
                builder.AppendLine($"{FunctionNativePtr} = CallGetNativeFunctionFromClassAndName({nativeType}, \"{SourceName}\");");
            }
        }

        if (HasParamsOrReturnValue)
        {
            builder.AppendLine($"{SizeVariableName} = UFunctionExporter.CallGetNativeFunctionParamsSize({functionPtrForOffsets});");
        }

        if (HasReturnValue)
        {
            ReturnType.ExportBackingVariablesToStaticConstructor(builder, functionPtrForOffsets);
        }

        base.ExportBackingVariablesToStaticConstructor(builder, functionPtrForOffsets);
    }

    public void ExportInvokeMethod(GeneratorStringBuilder builder)
    {
        builder.AppendEditorBrowsableAttribute();
        builder.AppendLine($"void Invoke_{SourceName}(IntPtr buffer, IntPtr returnBuffer)");
        
        if (!HasParamsOrReturnValue)
        {
            builder.Append(" => " + (NeedsImplementationFunction ? $"{SourceName}_Implementation();" : $"{SourceName}();"));
            return;
        }
        
        builder.OpenBrace();
        
        foreach (UnrealProperty parameter in Properties)
        {
            builder.AppendLine();
            parameter.ExportFromNative(builder, SourceGenUtilities.Buffer, $"{parameter.ManagedType} {parameter.SourceName} = ");
        }
        
        ExportInvokeMethodCallSignature(builder);
        
        if (HasReturnValue)
        {
            builder.AppendLine();
            ReturnType.ExportToNative(builder, "returnBuffer", "returnValue");
        }
        
        foreach (UnrealProperty parameter in Properties.Where(p => p.PropertyFlags.HasFlag(EPropertyFlags.OutParm)))
        {
            builder.AppendLine();
            parameter.ExportToNative(builder, SourceGenUtilities.Buffer, parameter.SourceName);
        }
        
        builder.CloseBrace();
    }

    protected virtual void ExportInvokeMethodCallSignature(GeneratorStringBuilder builder)
    {
        string returnAssignment = HasReturnValue ? $"{ReturnType.ManagedType} returnValue = " : string.Empty;
        string functionToCall = NeedsImplementationFunction ? $"{SourceName}_Implementation" : SourceName;
        builder.AppendLine($"{returnAssignment}{functionToCall}({GetParameterSignature()});");
    }

    protected string GetParameterSignature()
    {
        return string.Join(", ", Properties.Select(p => p.ReferenceKind.RefKindToString() + p.SourceName));
    }

    public void ExportImplementationMethod(GeneratorStringBuilder builder)
    {
        builder.AppendLine($"{TypeAccessibility.AccessibilityToString()}partial {ReturnType.ManagedType} {SourceName}_Implementation({string.Join(", ", Properties.Select(p => $"{p.ManagedType} {p.SourceName}"))});");
    }

    public void ExportWrapperMethod(GeneratorStringBuilder builder, string instanceFunction)
    {
        builder.AppendLine();
        
        builder.AppendLine($"{TypeAccessibility.AccessibilityToString()}partial {ReturnType.ManagedType} {SourceName}({string.Join(", ", Properties.Select(p => $"{p.ManagedType} {p.SourceName}"))})");
        builder.OpenBrace();

        if (FunctionFlags.HasFlag(EFunctionFlags.Event) || IsNetworkFunction)
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
    
    public void ExportCallToNative(GeneratorStringBuilder builder, Action<string, string> nativeCall)
    {
        if (!HasParamsOrReturnValue)
        {
            nativeCall(SourceGenUtilities.IntPtrZero, SourceGenUtilities.IntPtrZero);
            return;
        }
        
        builder.BeginUnsafeBlock();
        
        builder.AllocateParameterBuffer(SizeVariableName);

        foreach (UnrealProperty parameter in Properties)
        {
            builder.AppendLine();
            parameter.ExportToNative(builder, SourceGenUtilities.ParamsBuffer, parameter.SourceName);
        }

        string returnBuffer = HasReturnValue
            ? $"{SourceGenUtilities.ParamsBuffer} + " + ReturnType.OffsetVariable
            : SourceGenUtilities.IntPtrZero;
            
        nativeCall(SourceGenUtilities.ParamsBuffer, returnBuffer);
            
        if (HasReturnValue)
        {
            builder.AppendLine($"{ReturnType.ManagedType} returnValue = ");
            ReturnType.ExportFromNative(builder, SourceGenUtilities.ParamsBuffer);
            builder.AppendLine("return returnValue;");
        }
            
        builder.EndUnsafeBlock();
    }

    void AppendCallInvokeNativeFunction(GeneratorStringBuilder builder, string instanceFunction, string paramsBuffer, string returnBuffer)
    {
        builder.AppendLine("UObjectExporter.");
        
        if (IsNetworkFunction)
        {
            builder.Append("CallInvokeNativeNetFunction");
        }
        else if (HasParamsOrReturnValue)
        {
            builder.Append("CallInvokeNativeFunctionOutParms");
        }
        else
        {
            builder.Append("CallInvokeNativeFunction");
        }
        
        builder.Append($"(NativeObject, {instanceFunction}, {paramsBuffer}, {returnBuffer});");
    }

    public override void PopulateJsonObject(JsonObject jsonObject)
    {
        base.PopulateJsonObject(jsonObject);
        jsonObject.TrySetJsonEnum("FunctionFlags", FunctionFlags);
        
        if (HasReturnValue)
        {
            ReturnType.PopulateJsonWithUnrealType(jsonObject, "ReturnValue");
        }
    }
}
