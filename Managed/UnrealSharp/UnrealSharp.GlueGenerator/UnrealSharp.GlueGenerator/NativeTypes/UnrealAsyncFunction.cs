using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using UnrealSharp.GlueGenerator.NativeTypes.Properties;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public record UnrealAsyncFunction : UnrealFunctionBase
{
    private const string CancellationTokenType = "System.Threading.CancellationToken";
    private const string AsyncBaseClassName = "UCSCancellableAsyncAction";
    private const string AsyncBaseClassNamespace = "UnrealSharp.UnrealSharpCore";
    private const string CompletedEventName = "Completed";
    private const string FailedEventName = "Failed";
    private const string ExceptionParamName = "Exception";
    private const string ResultParamName = "Result";
    private const string DelegateFieldName = "Arg0";
    private const string TargetParamName = "Target";
    private const string FallbackErrorMessage = "An unknown error occurred during async operation.";
    private const string StaticOuterExpression = "UnrealSharp.Engine.UGameplayStatics.GameInstance";

    public string WrapperName => $"{Outer!.SourceName}_{SourceName}Action";
    public string WrapperDelegateName => $"F{Outer!.EngineName}_{SourceName}";

    public bool HasTaskResultReturnValue => ReturnType is TaskPropertyBase task && task.HasTemplateParameters;

    public bool HasVoidReturn => ReturnType is VoidProperty;

    public bool HasValueTaskReturn
    {
        get
        {
            string fullName = ReturnType.ManagedType.FullName;
            return fullName == "System.Threading.Tasks.ValueTask" 
                   || fullName.StartsWith("System.Threading.Tasks.ValueTask<") 
                   || fullName.StartsWith("System.Threading.Tasks.ValueTask`");
        }
    }

    public UnrealAsyncFunction(IMethodSymbol typeSymbol, UnrealType outer) : base(typeSymbol, outer)
    {
    }

    public override void ExportType(GeneratorStringBuilder generatorStringBuilder, SourceProductionContext spc)
    {
        bool hasCancellationToken = TryRemoveCancellationToken(Properties.List);

        GeneratorStringBuilder builder = new GeneratorStringBuilder();
        builder.BeginGeneratedSourceFile(this);

        AppendDelegateWrapper(builder, spc);

        UnrealClass asyncWrapperClass = CreateAsyncWrapperClass();

        TypeDeclarationBuilder
            .FromUnrealType(asyncWrapperClass, SourceGenUtilities.ClassKeyword)
            .Extends(asyncWrapperClass.FullParentName)
            .Build(builder);

        AppendWrapperVariables(builder, asyncWrapperClass, hasCancellationToken);
        AppendActivateOverride(builder);
        AppendCancelOverride(builder, hasCancellationToken);
        AppendRunAsync(builder, hasCancellationToken);
        AppendOnTaskCompleted(builder, hasCancellationToken);
        AppendAsyncFactoryFunction(builder, asyncWrapperClass, hasCancellationToken, spc);

        asyncWrapperClass.ExportList(builder, spc, asyncWrapperClass.Properties);
        AppendStaticConstructor(builder, asyncWrapperClass);

        builder.CloseBrace();
        
        builder.GenerateTypeRegistration(asyncWrapperClass);

        spc.AddSource($"{WrapperName}.g.cs", builder.ToString());
    }

    public override void ExportBackingVariablesToStaticConstructor(GeneratorStringBuilder builder, string nativeType)
    {
    }

    public override void ExportBackingVariables(GeneratorStringBuilder builder)
    {
    }

    private static bool TryRemoveCancellationToken(List<UnrealProperty> properties)
    {
        int index = properties.FindIndex(p => p.ManagedType.FullName == CancellationTokenType);

        if (index < 0)
        {
            return false;
        }

        properties.RemoveAt(index);
        return true;
    }

    private UnrealClass CreateAsyncWrapperClass()
    {
        UnrealClass asyncWrapperClass = new UnrealClass(
            EClassFlags.None,
            AsyncBaseClassName,
            AsyncBaseClassNamespace,
            WrapperName,
            Outer!.Namespace,
            Accessibility.Public,
            AssemblyName)
        {
            Overrides = new EquatableList<string>(["ReceiveActivate", "ReceiveCancel"]),
        };

        asyncWrapperClass.AddMetaData("HasDedicatedAsyncNode", "true");
        Outer.AddSourceGeneratorDependency(asyncWrapperClass);
        return asyncWrapperClass;
    }

    private void AppendDelegateWrapper(GeneratorStringBuilder builder, SourceProductionContext spc)
    {
        UnrealDelegateFunction delegateFunction = new UnrealDelegateFunction(
            EFunctionFlags.None,
            WrapperDelegateName,
            Outer!.Namespace,
            Accessibility.Public,
            Outer.AssemblyName,
            Outer);

        StringProperty exceptionParam = new StringProperty(ExceptionParamName, Accessibility.Public, delegateFunction);
        exceptionParam.MakeParameter();

        List<UnrealProperty> properties = new List<UnrealProperty>(capacity: 2);

        if (ReturnType is TaskPropertyBase taskReturn && taskReturn.HasTemplateParameters)
        {
            UnrealProperty resultParam = taskReturn.TemplateParameters[0];
            resultParam.SourceName = ResultParamName;
            resultParam.PropertyFlags = 0;
            resultParam.MakeParameter();
            properties.Add(resultParam);
        }

        properties.Add(exceptionParam);

        delegateFunction.Properties = new EquatableList<UnrealProperty>(properties);
        delegateFunction.ReturnType = new VoidProperty(delegateFunction);

        UnrealDelegate asyncDelegate = new UnrealDelegate(delegateFunction, true);
        delegateFunction.Outer = asyncDelegate;

        asyncDelegate.AppendFunctionAsDelegate(builder);
        asyncDelegate.ExportType(builder, spc);
        
        builder.AppendLine();

        Outer.AddSourceGeneratorDependency(asyncDelegate);
    }

    private void AppendWrapperVariables(GeneratorStringBuilder builder, UnrealClass asyncWrapperClass, bool hasCancellationToken)
    {
        MulticastDelegateProperty completed = CreateMulticastDelegate(asyncWrapperClass, CompletedEventName);
        MulticastDelegateProperty failed = CreateMulticastDelegate(asyncWrapperClass, FailedEventName);
        asyncWrapperClass.Properties = new EquatableList<UnrealProperty>([completed, failed]);

        if (!HasVoidReturn)
        {
            builder.AppendLine($"private {ReturnType.ManagedType}? _task;");
        }

        if (hasCancellationToken)
        {
            builder.AppendLine("private readonly CancellationTokenSource _cancellationTokenSource = new();");
        }

        if (HasVoidReturn)
        {
            builder.AppendLine(hasCancellationToken ? "public Action<CancellationToken>? asyncDelegate;" : "public Action? asyncDelegate;");
        }
        else
        {
            string ctTypeArg = hasCancellationToken ? "CancellationToken, " : string.Empty;
            builder.AppendLine($"public Func<{ctTypeArg}{ReturnType.ManagedType}>? asyncDelegate;");
        }
    }

    private MulticastDelegateProperty CreateMulticastDelegate(UnrealClass asyncWrapperClass, string eventName)
    {
        FieldName unrealDelegateType = new FieldName(
            DelegateProperty.MakeDelegateSignatureName(WrapperDelegateName),
            asyncWrapperClass.Namespace,
            asyncWrapperClass.AssemblyName);

        FieldName managedDelegateType = new FieldName(
            WrapperDelegateName,
            asyncWrapperClass.Namespace,
            asyncWrapperClass.AssemblyName);

        FieldProperty signatureField = new FieldProperty(
            PropertyType.SignatureDelegate,
            unrealDelegateType,
            managedDelegateType,
            DelegateFieldName,
            Accessibility.Public,
            asyncWrapperClass);

        MulticastDelegateProperty multicast = new MulticastDelegateProperty(
            new EquatableArray<UnrealProperty>([signatureField]),
            eventName,
            Accessibility.Public,
            asyncWrapperClass);

        multicast.MakeBlueprintAssignable();
        return multicast;
    }

    private static void AppendActivateOverride(GeneratorStringBuilder builder)
    {
        builder.AppendLine("public override void Activate()");
        builder.OpenBrace();
        builder.AppendLine("RunAsync();");
        builder.CloseBrace();
    }

    private static void AppendCancelOverride(GeneratorStringBuilder builder, bool hasCancellationToken)
    {
        builder.AppendLine("public override void Cancel()");
        builder.OpenBrace();

        if (hasCancellationToken)
        {
            builder.AppendLine("_cancellationTokenSource.Cancel();");
        }

        builder.AppendLine("base.Cancel();");
        builder.CloseBrace();
    }

    private void AppendRunAsync(GeneratorStringBuilder builder, bool hasCancellationToken)
    {
        string cancellationTokenArg = hasCancellationToken ? "_cancellationTokenSource.Token" : string.Empty;
        string taskValueAccess = HasValueTaskReturn ? "_task.Value" : "_task";

        builder.AppendLine("async void RunAsync()");
        builder.OpenBrace();
        builder.AppendLine("if (asyncDelegate == null) { throw new InvalidOperationException(\"AsyncDelegate was null\"); }");

        builder.AppendLine("try");
        builder.OpenBrace();
        if (HasVoidReturn)
        {
            builder.AppendLine($"asyncDelegate({cancellationTokenArg});");
        }
        else
        {
            builder.AppendLine($"_task = asyncDelegate({cancellationTokenArg});");
            builder.AppendLine($"await {taskValueAccess}.ConfigureWithUnrealContext();");
        }
        builder.CloseBrace();

        builder.AppendLine("catch (OperationCanceledException)");
        builder.OpenBrace();
        builder.AppendLine("Cancel();");
        builder.AppendLine("return;");
        builder.CloseBrace();

        builder.AppendLine("catch (Exception e)");
        builder.OpenBrace();
        builder.AppendLine(HasVoidReturn ? "OnTaskCompleted(e.ToString());" : "OnTaskCompleted(_task, e.ToString());");
        builder.AppendLine("return;");
        builder.CloseBrace();

        builder.AppendLine(HasVoidReturn ? "OnTaskCompleted(string.Empty);" : "OnTaskCompleted(_task, string.Empty);");
        builder.CloseBrace();
    }

    private void AppendOnTaskCompleted(GeneratorStringBuilder builder, bool hasCancellationToken)
    {
        builder.AppendLine(HasVoidReturn ? "void OnTaskCompleted(string? exception)" : $"void OnTaskCompleted({ReturnType.ManagedType}? t, string? exception)");
        builder.OpenBrace();

        if (hasCancellationToken)
        {
            builder.AppendLine("if (_cancellationTokenSource.IsCancellationRequested) { return; }");
        }

        builder.AppendLine("if (IsDestroyed) { return; }");

        string taskValueAccess = HasValueTaskReturn ? "t.Value" : "t";
        string faultedCondition = HasVoidReturn ? "!string.IsNullOrEmpty(exception)" : $"t is null || {taskValueAccess}.IsFaulted || !string.IsNullOrEmpty(exception)";
        builder.AppendLine($"if ({faultedCondition})");
        builder.OpenBrace();
        string defaultResultArg = HasTaskResultReturnValue ? "default!, " : string.Empty;
        builder.AppendLine($"Failed?.InnerDelegate.Invoke({defaultResultArg}exception ?? \"{FallbackErrorMessage}\");");
        builder.CloseBrace();

        builder.AppendLine("else");
        builder.OpenBrace();
        builder.AppendLine(HasTaskResultReturnValue ? $"Completed?.InnerDelegate.Invoke({taskValueAccess}.Result, string.Empty);" : "Completed?.InnerDelegate.Invoke(null);");
        builder.CloseBrace();

        builder.CloseBrace();
    }

    private void AppendAsyncFactoryFunction(GeneratorStringBuilder builder, UnrealClass asyncWrapperClass, bool hasCancellationToken, SourceProductionContext spc)
    {
        bool isStatic = FunctionFlags.HasFlag(EFunctionFlags.Static);
        UnrealFunctionBase factory = BuildFactoryFunctionMetadata(asyncWrapperClass, isStatic);

        string parameterList = string.Join(", ", factory.Properties.Select(p => $"{p.ManagedType} {p.SourceName}"));
        string ownerExpression = isStatic ? StaticOuterExpression : TargetParamName;
        string callTarget = isStatic ? Outer!.SourceName : TargetParamName;
        string arguments = BuildArguments(factory, hasCancellationToken);
        string lambdaHeader = hasCancellationToken ? "cancellationToken => " : "() => ";

        builder.AppendLine($"public static {WrapperName} {SourceName}({parameterList})");
        builder.OpenBrace();
        builder.AppendLine($"var action = NewObject<{WrapperName}>({ownerExpression});");
        builder.AppendLine($"action.asyncDelegate = {lambdaHeader}{callTarget}.{SourceName}({arguments});");
        builder.AppendLine("return action;");
        builder.CloseBrace();

        factory.ExportType(builder, spc);
        asyncWrapperClass.AddFunction(factory);
    }

    private UnrealFunctionBase BuildFactoryFunctionMetadata(UnrealClass asyncWrapperClass, bool isStatic)
    {
        UnrealFunction factory = new UnrealFunction(
            EFunctionFlags.BlueprintCallable | EFunctionFlags.Static,
            SourceName,
            Namespace,
            Accessibility.Public,
            AssemblyName,
            asyncWrapperClass);

        FieldName wrapperType = new FieldName(WrapperName, Namespace, AssemblyName);
        ObjectProperty returnValue = new ObjectProperty(wrapperType, SourceGenUtilities.ReturnValueName, Accessibility.NotApplicable, factory);
        returnValue.MakeReturnParameter();

        List<UnrealProperty> factoryParameters = Properties.List;
        foreach (UnrealProperty parameter in factoryParameters)
        {
            parameter.Outer = asyncWrapperClass;
        }

        if (!isStatic)
        {
            FieldName targetType = new FieldName(Outer!.SourceName, Outer.Namespace, Outer.AssemblyName);
            ObjectProperty targetParam = new ObjectProperty(targetType, TargetParamName, Accessibility.NotApplicable, factory);
            targetParam.MakeParameter();

            factoryParameters.Insert(0, targetParam);
        }

        factory.Properties = new EquatableList<UnrealProperty>(factoryParameters);
        factory.ReturnType = returnValue;

        factory.AddMetaData("DefaultToSelf", TargetParamName);
        factory.AddMetaData("BlueprintInternalUseOnly", "true");
        factory.AddMetaDataRange(MetaData);

        return factory;
    }

    private string BuildArguments(UnrealFunctionBase factory, bool hasCancellationToken)
    {
        string arguments = HasParams ? string.Join(", ", factory.Properties.Where(p => p.SourceName != TargetParamName).Select(p => p.SourceName)) : string.Empty;

        if (!hasCancellationToken)
        {
            return arguments;
        }

        return arguments.Length > 0 ? $"{arguments}, cancellationToken" : "cancellationToken";
    }

    private static void AppendStaticConstructor(GeneratorStringBuilder builder, UnrealClass asyncWrapperClass)
    {
        builder.BeginTypeStaticConstructor(asyncWrapperClass);
        asyncWrapperClass.ExportBackingVariablesToStaticConstructor(builder, SourceGenUtilities.NativeTypePtr);
        builder.EndTypeStaticConstructor();
    }
}