using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

public enum ReturnValueType
{
    Void,
    Value,
    AsyncTask,
    ValueTask
}

[Inspector]
public abstract record UnrealFunctionBase : UnrealStruct
{
    public override string EngineName => SourceName;
    
    public UnrealProperty ReturnType;
    public EFunctionFlags FunctionFlags;
    public ReturnValueType ReturnValueType;

    protected static readonly EFunctionFlags NetFunctionFlags = EFunctionFlags.NetServer | EFunctionFlags.NetClient | EFunctionFlags.NetMulticast;
    protected const string UFunctionAttributeName = "UFunctionAttribute";
    
    protected bool IsNetFunction => (FunctionFlags & NetFunctionFlags) != EFunctionFlags.None;

    protected bool NeedsImplementationFunction => IsNetFunction || FunctionFlags.HasFlag(EFunctionFlags.BlueprintNativeEvent);
    protected bool IsEvent => FunctionFlags.HasFlag(EFunctionFlags.Event);
    protected bool HasParamsOrReturnValue => HasParams || HasReturnValue;
    protected bool HasParams => Properties.Count > 0;
    protected bool HasReturnValue => ReturnType is not VoidProperty;
    
    public string SizeVariableName => $"{SourceName}_Size";
    public string FunctionNativePtr => $"{SourceName}Ptr";

    public UnrealFunctionBase(SemanticModel model, ISymbol typeSymbol, MethodDeclarationSyntax syntax, UnrealType outer) : this(model, typeSymbol, syntax.ReturnType, syntax, syntax.ParameterList.Parameters, outer!)
    {
        IMethodSymbol methodSymbol = (IMethodSymbol) typeSymbol;
        
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
    
    public UnrealFunctionBase(SemanticModel model, ISymbol typeSymbol, DelegateDeclarationSyntax syntax, UnrealType outer) : this(model, typeSymbol, syntax.ReturnType, syntax, syntax.ParameterList.Parameters, outer!)
    {
        
    }
    
    public UnrealFunctionBase(SemanticModel model, ISymbol typeSymbol, TypeSyntax returnType, SyntaxNode syntax, SeparatedSyntaxList<ParameterSyntax> parameterList, UnrealType outer) : base(typeSymbol, syntax, outer)
    {
        ITypeSymbol returnTypeSymbol = model.GetTypeInfo(returnType).Type!;

        bool hasOutParams = false;
        if (returnTypeSymbol.Name == VoidProperty.VoidTypeName)
        {
            ReturnType = new VoidProperty(this);
            ReturnValueType = ReturnValueType.Void;
        }
        else
        {
            ISymbol returnValueSymbol;
            if (typeSymbol is IMethodSymbol methodSymbol && methodSymbol.IsAsync)
            {
                INamedTypeSymbol taskSymbol = model.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1")!;
                INamedTypeSymbol valueTaskSymbol = model.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1")!;
                
                bool isValueTask = returnTypeSymbol.OriginalDefinition.Equals(valueTaskSymbol, SymbolEqualityComparer.Default);
                bool isTask = returnTypeSymbol.OriginalDefinition.Equals(taskSymbol, SymbolEqualityComparer.Default);
                
                if (isValueTask || isTask)
                {
                    ITypeSymbol taskReturnType = returnTypeSymbol is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1
                        ? namedType.TypeArguments[0]
                        : model.Compilation.GetSpecialType(SpecialType.System_Void);
                    
                    returnValueSymbol = taskReturnType;
                    returnTypeSymbol = taskReturnType;
                    
                    ReturnValueType = isValueTask ? ReturnValueType.ValueTask : ReturnValueType.AsyncTask;
                }
                else
                {
                    returnValueSymbol = returnTypeSymbol;
                    ReturnValueType = ReturnValueType.Value;
                }
            }
            else
            {
                returnValueSymbol = model.GetSymbolInfo(returnType).Symbol!;
                ReturnValueType = ReturnValueType.Value;
            }

            ReturnType = PropertyFactory.CreateProperty(returnTypeSymbol, returnType, returnValueSymbol, this);
            ReturnType.SourceName = "ReturnValue";
            ReturnType.MakeReturnParameter();
            
            hasOutParams = true;
        }

        if (parameterList.Count <= 0)
        {
            return;
        }
        
        List<UnrealProperty> parameters = new List<UnrealProperty>(parameterList.Count);
        bool paramHasDefaults = false;
        
        for (int i = 0; i < parameterList.Count; i++)
        {
            ParameterSyntax parameterSyntax = parameterList[i];
            IParameterSymbol parameterSymbol = model.GetDeclaredSymbol(parameterSyntax)!;
                
            UnrealProperty property = PropertyFactory.CreateProperty(parameterSymbol.Type, parameterSyntax, parameterSymbol, this);
            property.RefKind = parameterSymbol.RefKind;
                
            property.PropertyFlags |= EPropertyFlags.Parm | EPropertyFlags.BlueprintVisible | EPropertyFlags.BlueprintReadOnly;

            switch (parameterSymbol.RefKind)
            {
                case RefKind.Out:
                    property.PropertyFlags |= EPropertyFlags.OutParm;
                    hasOutParams = true;
                    break;
                case RefKind.Ref:
                    property.PropertyFlags |= EPropertyFlags.OutParm | EPropertyFlags.ReferenceParm;
                    hasOutParams = true;
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
                paramHasDefaults = true;
            }

            parameters.Add(property);
        }
        
        if (paramHasDefaults)
        {
            FunctionFlags |= EFunctionFlags.HasDefaults;
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
    public static UnrealType? UFunctionAttribute(UnrealType? outer, GeneratorAttributeSyntaxContext ctx, MemberDeclarationSyntax memberDeclaration, IReadOnlyList<AttributeData> attributes)
    {
        UnrealClassBase unrealClass = (UnrealClassBase) outer!;
        IMethodSymbol methodSymbol = (IMethodSymbol) ctx.SemanticModel.GetDeclaredSymbol(memberDeclaration)!;
        MethodDeclarationSyntax methodDeclaration = (MethodDeclarationSyntax) memberDeclaration;

        UnrealFunctionBase unrealFunction;
        if (methodSymbol.IsAsync)
        {
            UnrealAsyncFunction asyncFunction = new UnrealAsyncFunction(ctx.SemanticModel, methodSymbol, methodDeclaration, outer!);
            unrealClass.AddAsyncFunction(asyncFunction);
            
            outer!.SourceGeneratorDependencies.List.Add(new FieldName(asyncFunction.WrapperName, outer.Namespace, outer.AssemblyName));
            unrealFunction = asyncFunction;
        }
        else
        {
            unrealFunction = new UnrealFunction(ctx.SemanticModel, methodSymbol, methodDeclaration, outer!);
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

        if (unrealFunction.IsNetFunction)
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

    public void ExportBackingVariables(GeneratorStringBuilder builder)
    {
        if (HasParamsOrReturnValue)
        {
            builder.AppendNewBackingField($"static int {SourceName}_Size = UFunctionExporter.CallGetNativeFunctionParamsSize({FunctionNativePtr});");
        }
            
        foreach (UnrealProperty parameter in Properties)
        {
            parameter.ExportBackingVariables(builder, FunctionNativePtr);
        }
            
        if (HasReturnValue)
        {
            ReturnType.ExportBackingVariables(builder, FunctionNativePtr);
        }
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
            builder.AppendLine($"{parameter.ManagedType} {parameter.SourceName} ");
            parameter.ExportFromNative(builder, SourceGenUtilities.Buffer, "= ");
        }
        
        builder.AppendLine();
        
        if (HasReturnValue)
        {
            builder.AppendLine($"{ReturnType.ManagedType} returnValue = ");
        }
        
        string functionToCall = NeedsImplementationFunction ? $"{SourceName}_Implementation" : SourceName;
        builder.Append($"{functionToCall}({string.Join(", ", Properties.Select(p => p.RefKind.RefKindToString() + p.SourceName))});");
        
        if (HasReturnValue)
        {
            builder.AppendLine();
            ReturnType.ExportToNative(builder, "returnBuffer", "returnValue");
        }
        
        builder.CloseBrace();
    }

    public void ExportImplementationMethod(GeneratorStringBuilder builder)
    {
        builder.AppendLine($"{Protection.AccessibilityToString()}partial {ReturnType.ManagedType} {SourceName}_Implementation({string.Join(", ", Properties.Select(p => $"{p.ManagedType} {p.SourceName}"))});");
    }

    public void ExportWrapperMethod(GeneratorStringBuilder builder, string instanceFunction)
    {
        builder.AppendLine();
        
        builder.AppendLine($"{Protection.AccessibilityToString()}partial {ReturnType.ManagedType} {SourceName}({string.Join(", ", Properties.Select(p => $"{p.ManagedType} {p.SourceName}"))})");
        builder.OpenBrace();

        if (FunctionFlags.HasFlag(EFunctionFlags.Event))
        {
            builder.AppendLine($"if ({instanceFunction} == IntPtr.Zero)");
            builder.OpenBrace();
            builder.AppendLine($"{instanceFunction} = CallGetNativeFunctionFromInstanceAndName(NativeObject, \"{SourceName}\");");
            builder.CloseBrace();
            
            builder.AppendLine($"if (!UFunctionExporter.IsFunctionImplemented({instanceFunction}))");
            builder.OpenBrace();
            builder.AppendLine($"throw new InvalidOperationException($\"Function {SourceName} is not implemented on instance of {Outer!.SourceName}.\");");
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
        
        if (IsNetFunction)
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
        jsonObject.TrySetEnum("FunctionFlags", FunctionFlags);
        
        if (HasReturnValue)
        {
            ReturnType.PopulateWithUnrealType(jsonObject, "ReturnValue");
        }
    }
}