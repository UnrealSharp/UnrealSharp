using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using UnrealSharp.GlueGenerator.NativeTypes.Properties;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public record UnrealAsyncFunction : UnrealFunctionBase
{
    const string CancellationTokenType = "System.Threading.CancellationToken";
    public string WrapperName => $"{Outer!.SourceName}_{SourceName}Action";
    
    public bool HasTaskResultReturnValue => ReturnType is not VoidProperty && ReturnType is TaskPropertyBase taskProperty && taskProperty.HasTemplateParameters;
    
    public UnrealAsyncFunction(IMethodSymbol typeSymbol, UnrealType outer) : base(typeSymbol, outer)
    {
        
    }
    
    public override void ExportType(GeneratorStringBuilder generatorStringBuilder, SourceProductionContext spc)
    {
        List<UnrealProperty> properties = Properties.List;
        bool hasCancellationToken = false;
        
        for (int i = 0; i < properties.Count; i++)
        {
            UnrealProperty property = properties[i];
            
            if (property.ManagedType.FullName != CancellationTokenType)
            {
                continue;
            }
            
            properties.RemoveAt(i);
            hasCancellationToken = true;
            break;
        }
        
        GeneratorStringBuilder builder = new GeneratorStringBuilder();
        
        builder.BeginGeneratedSourceFile(this);

        string wrapperName = WrapperName;
        string wrapperDelegateName = $"F{Outer!.EngineName}_{SourceName}";
        
        AppendDelegateWrapper(builder, ReturnType, wrapperDelegateName, spc);
        
        UnrealClass asyncWrapperClass = new UnrealClass(EClassFlags.None, "UCSCancellableAsyncAction", "UnrealSharp.UnrealSharpCore", wrapperName, Outer.Namespace, Accessibility.Public, AssemblyName)
        {
            Overrides = new EquatableList<string>(["ReceiveActivate", "ReceiveCancel"])
        };
        
        asyncWrapperClass.AddMetaData("HasDedicatedAsyncNode", "true");
        
        Outer.AddSourceGeneratorDependency(asyncWrapperClass);

        builder.BeginType(asyncWrapperClass, SourceGenUtilities.ClassKeyword, baseType: asyncWrapperClass.FullParentName);
        
        AppendWrapperVariables(builder, asyncWrapperClass, wrapperDelegateName, hasCancellationToken);
        
        AppendActivateOverride(builder);
        AppendCancelOverride(builder, hasCancellationToken);
        
        AppendRunAsync(builder, hasCancellationToken);
        
        AppendOnTaskCompletedFunction(builder, hasCancellationToken);
        
        AppendAsyncFactoryFunction(builder, properties, asyncWrapperClass, hasCancellationToken, spc, wrapperName);
        
        asyncWrapperClass.ExportList(builder, spc, asyncWrapperClass.Properties);
        
        AppendStaticConstructor(builder, asyncWrapperClass);
        
        builder.CloseBrace();
        
        builder.GenerateTypeRegistration(asyncWrapperClass); 
        
        spc.AddSource($"{wrapperName}.g.cs", builder.ToString());
    }

    public override void ExportBackingVariablesToStaticConstructor(GeneratorStringBuilder builder, string nativeType)
    {
        
    }

    public override void ExportBackingVariables(GeneratorStringBuilder builder)
    {
        
    }

    void AppendDelegateWrapper(GeneratorStringBuilder builder, UnrealProperty returnValue, string wrapperDelegateName, SourceProductionContext spc)
    {
        UnrealDelegateFunction delegateFunction = new UnrealDelegateFunction(EFunctionFlags.None,
            wrapperDelegateName, Outer!.Namespace, Accessibility.Public, Outer.AssemblyName, Outer);
        
        StringProperty stringProperty = new StringProperty("Exception", Accessibility.Public, delegateFunction);
        stringProperty.MakeParameter();

        List<UnrealProperty> properties = new List<UnrealProperty>(2);
        
        if (returnValue is not VoidProperty && returnValue is TaskPropertyBase taskProperty && taskProperty.HasTemplateParameters)
        {
            returnValue = taskProperty.TemplateParameters[0];
            
            properties.Add(returnValue);
            returnValue.SourceName = "Result";
            
            returnValue.PropertyFlags = 0;
            returnValue.MakeParameter();
        }
        
        properties.Add(stringProperty);
        
        delegateFunction.Properties = new EquatableList<UnrealProperty>(properties);
        delegateFunction.ReturnType = new VoidProperty(delegateFunction);
        
        UnrealDelegate asyncDelegate = new UnrealDelegate(delegateFunction, true);
        delegateFunction.Outer = asyncDelegate;
        
        asyncDelegate.AppendFunctionAsDelegate(builder);
        asyncDelegate.ExportType(builder, spc);
        
        builder.GenerateTypeRegistration(asyncDelegate);
        builder.AppendLine();
        
        Outer.AddSourceGeneratorDependency(asyncDelegate);
    }
    
    void AppendWrapperVariables(GeneratorStringBuilder builder, UnrealClass asyncWrapperClass, string wrapperDelegateName, bool hasCancellationToken)
    {
        FieldName unrealDelegateType = new FieldName(DelegateProperty.MakeDelegateSignatureName(wrapperDelegateName), asyncWrapperClass.Namespace, asyncWrapperClass.AssemblyName);
        FieldName managedDelegateType = new FieldName(wrapperDelegateName, asyncWrapperClass.Namespace, asyncWrapperClass.AssemblyName);
        
        FieldProperty fieldProperty = new FieldProperty(PropertyType.SignatureDelegate, unrealDelegateType, managedDelegateType, "Arg0", Accessibility.Public, asyncWrapperClass);
        EquatableArray<UnrealProperty> properties = new EquatableArray<UnrealProperty>([fieldProperty]);
        
        MulticastDelegateProperty completedProperty = new MulticastDelegateProperty(properties, "Completed", Accessibility.Public, asyncWrapperClass);
        completedProperty.MakeBlueprintAssignable();
        
        FieldName failedPropertyFunction = new FieldName(DelegateProperty.MakeDelegateSignatureName(wrapperDelegateName), asyncWrapperClass.Namespace, asyncWrapperClass.AssemblyName);
        
        FieldProperty failedFieldProperty = new FieldProperty(PropertyType.SignatureDelegate, failedPropertyFunction, managedDelegateType, "Arg0", Accessibility.Public, asyncWrapperClass);
        EquatableArray<UnrealProperty> failedProperties = new EquatableArray<UnrealProperty>([failedFieldProperty]);
        
        MulticastDelegateProperty failedProperty = new MulticastDelegateProperty(failedProperties, "Failed", Accessibility.Public, asyncWrapperClass);
        failedProperty.MakeBlueprintAssignable();
        
        asyncWrapperClass.Properties = new EquatableList<UnrealProperty>([completedProperty, failedProperty]);
        
        builder.AppendLine($"private {ReturnType.ManagedType} _task;");
        
        if (hasCancellationToken)
        {
            builder.AppendLine("private readonly CancellationTokenSource _cancellationTokenSource = new();");
        }

        string cancellationTokenAnnotation = hasCancellationToken ? "CancellationToken, " : string.Empty;
        builder.AppendLine($"public Func<{cancellationTokenAnnotation}{ReturnType.ManagedType}>? asyncDelegate;");
    }

    void AppendActivateOverride(GeneratorStringBuilder builder)
    {
        builder.AppendLine("public override void Activate()");
        builder.OpenBrace();
        builder.AppendLine("RunAsync();");
        builder.CloseBrace();
    }

    void AppendRunAsync(GeneratorStringBuilder builder, bool hasCancellationToken)
    {
        builder.AppendLine("async void RunAsync()");
        builder.OpenBrace();
        
        builder.AppendLine("if (asyncDelegate == null) { throw new InvalidOperationException(\"AsyncDelegate was null\"); }");
        
        builder.AppendLine("try");
        builder.OpenBrace();
        
        string cancellationTokenArgument = hasCancellationToken ? "_cancellationTokenSource.Token" : string.Empty;
        
        builder.AppendLine($"_task = asyncDelegate({cancellationTokenArgument});");
        builder.AppendLine("await _task.ConfigureWithUnrealContext();");
        builder.CloseBrace();
        
        builder.AppendLine("catch (OperationCanceledException)");
        builder.OpenBrace();
        builder.AppendLine("Cancel();");
        builder.AppendLine("return;");
        builder.CloseBrace();
        builder.AppendLine("catch (Exception e)");
        builder.OpenBrace();
        builder.AppendLine("OnTaskCompleted(_task, e.ToString());");
        builder.AppendLine("return;");
        builder.CloseBrace();
        
        builder.AppendLine("OnTaskCompleted(_task, string.Empty);");
        
        builder.CloseBrace();
    }

    void AppendCancelOverride(GeneratorStringBuilder builder, bool hasCancellationToken)
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

    void AppendOnTaskCompletedFunction(GeneratorStringBuilder builder, bool hasCancellationToken)
    {
        builder.AppendLine($"void OnTaskCompleted({ReturnType.ManagedType} t, string? exception)");
        builder.OpenBrace();
        
        if (hasCancellationToken)
        {
            builder.AppendLine("if (_cancellationTokenSource.IsCancellationRequested) { return; }");
        }
        
        builder.AppendLine("if (IsDestroyed) { return; }");
        builder.AppendLine("if (t.IsFaulted || !string.IsNullOrEmpty(exception))");
        builder.OpenBrace();
        
        string returnStatement = HasTaskResultReturnValue ? "default!, " : string.Empty;

        string exception = "exception != null ? exception : \"An unknown error occurred during async operation.\"";
        builder.AppendLine($"Failed?.InnerDelegate.Invoke({returnStatement}{exception});");
        builder.CloseBrace();
        builder.AppendLine("else");
        builder.OpenBrace();
        
        if (HasTaskResultReturnValue)
        {
            builder.AppendLine("Completed?.InnerDelegate.Invoke(t.Result, string.Empty);");
        }
        else
        {
            builder.AppendLine("Completed?.InnerDelegate.Invoke(null);");
        }
        
        builder.CloseBrace();
        builder.CloseBrace();
    }
    
    void AppendAsyncFactoryFunction(GeneratorStringBuilder builder, List<UnrealProperty> properties, UnrealClass asyncWrapperClass, bool hasCancellationToken, SourceProductionContext spc, string wrapperName)
    {
        UnrealFunctionBase asyncFactoryFunction = new UnrealFunction(EFunctionFlags.BlueprintCallable | EFunctionFlags.Static, 
            SourceName, 
            Namespace, 
            Accessibility.Public, 
            AssemblyName,
            asyncWrapperClass);
        
        FieldName asyncWrapperType = new FieldName(wrapperName, Namespace, AssemblyName);
        ObjectProperty returnValue = new ObjectProperty(asyncWrapperType, SourceGenUtilities.ReturnValueName, Accessibility.NotApplicable, asyncFactoryFunction);
        returnValue.MakeReturnParameter();
        
        FieldName targetType = new FieldName(Outer!.SourceName, Outer.Namespace, Outer.AssemblyName);
        ObjectProperty targetParam = new ObjectProperty(targetType, "Target", Accessibility.NotApplicable, asyncFactoryFunction);
        targetParam.MakeParameter();
        
        foreach (UnrealProperty parameter in properties)
        {
            parameter.Outer = asyncWrapperClass;
        }
        
        properties.Insert(0, targetParam);

        asyncFactoryFunction.Properties = new EquatableList<UnrealProperty>(properties);
        asyncFactoryFunction.ReturnType = returnValue;
        
        asyncFactoryFunction.AddMetaData("DefaultToSelf", "Target");
        asyncFactoryFunction.AddMetaData("BlueprintInternalUseOnly", "true");
        asyncFactoryFunction.AddMetaDataRange(MetaData);
        
        builder.AppendLine("public static " + wrapperName + " " + SourceName + "(" + string.Join(", ", asyncFactoryFunction.Properties.Select(p => $"{p.ManagedType} {p.SourceName}")) + ")");
        builder.OpenBrace();
        builder.AppendLine($"var action = NewObject<{wrapperName}>(Target);");
        
        string parametersSignature = HasParams ? string.Join(", ", asyncFactoryFunction.Properties.Skip(1).Select(p => p.SourceName)) : string.Empty;
        
        builder.AppendLine("action.asyncDelegate = ");
        
        if (hasCancellationToken)
        { 
            builder.Append("cancellationToken => ");
        }
        else
        {
            builder.Append("() => ");
        }
        
        builder.Append($"Target.{SourceName}");
        
        builder.Append("(");
        builder.Append(parametersSignature);
        
        if (hasCancellationToken)
        {
            if (parametersSignature.Length > 0)
            {
                builder.Append(", ");
            }
            
            builder.Append("cancellationToken");
        }
        
        builder.Append(");");
        
        builder.AppendLine("return action;");
        builder.CloseBrace();
        
        asyncFactoryFunction.ExportType(builder, spc);
        
        asyncWrapperClass.AddFunction(asyncFactoryFunction);
    }

    void AppendStaticConstructor(GeneratorStringBuilder builder, UnrealClass asyncWrapperClass)
    {
        builder.BeginTypeStaticConstructor(asyncWrapperClass);
        asyncWrapperClass.ExportBackingVariablesToStaticConstructor(builder, SourceGenUtilities.NativeTypePtr);
        builder.EndTypeStaticConstructor();
    }
}