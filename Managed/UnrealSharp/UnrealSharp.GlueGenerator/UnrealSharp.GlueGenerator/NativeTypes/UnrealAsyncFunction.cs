using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnrealSharp.GlueGenerator.NativeTypes.Properties;

namespace UnrealSharp.GlueGenerator.NativeTypes;

public record UnrealAsyncFunction : UnrealFunctionBase
{
    const string CancellationTokenType = "System.Threading.CancellationToken";
    
    public UnrealAsyncFunction(SemanticModel model, ISymbol typeSymbol, MethodDeclarationSyntax syntax, UnrealType outer) : base(model, typeSymbol, syntax, outer)
    {
        
    }
    
    public override void ExportType(GeneratorStringBuilder generatorStringBuilder, SourceProductionContext spc)
    {
        List<UnrealProperty> properties = Properties.List;
        bool hasCancellationToken = false;
        
        for (int i = 0; i < properties.Count; i++)
        {
            UnrealProperty property = properties[i];
            
            if (property.ManagedType != CancellationTokenType)
            {
                continue;
            }
            
            properties.RemoveAt(i);
            hasCancellationToken = true;
            break;
        }
        
        GeneratorStringBuilder builder = new GeneratorStringBuilder();
        builder.BeginGeneratedSourceFile(this);
        
        string wrapperName = $"{Outer!.SourceName}_{SourceName}Action";
        string wrapperDelegateName = $"{Outer.SourceName}{SourceName}__DelegateSignature";
        
        AppendDelegateWrapper(builder, ReturnType, wrapperDelegateName, spc);
        
        UnrealClass asyncWrapperClass = new UnrealClass(EClassFlags.None, "UCSCancellableAsyncAction", "UnrealSharp.UnrealSharpCore", wrapperName,
            Outer.Namespace, Accessibility.Public, AssemblyName)
        {
            Overrides = new EquatableList<string>(["ReceiveActivate", "ReceiveCancel"])
        };

        builder.BeginType(asyncWrapperClass, TypeKind.Class, baseType: asyncWrapperClass.FullParentName);
        
        AppendWrapperVariables(builder, asyncWrapperClass, wrapperDelegateName, hasCancellationToken);
        
        AppendActivateOverride(builder);
        AppendCancelOverride(builder, hasCancellationToken);
        
        AppendOnTaskCompletedFunction(builder, hasCancellationToken);
        
        AppendAsyncWrapperMetaData(builder, properties, asyncWrapperClass, hasCancellationToken, spc, wrapperName);
        
        builder.CloseBrace();
        
        builder.BeginModuleInitializer(asyncWrapperClass); 
        spc.AddSource($"{wrapperName}.g.cs", builder.ToString());
    }

    void AppendDelegateWrapper(GeneratorStringBuilder builder, UnrealProperty returnValue, string wrapperDelegateName, SourceProductionContext spc)
    {
        UnrealFunctionBase delegateFunction = new UnrealFunction(EFunctionFlags.None,
            wrapperDelegateName, Outer!.Namespace, Accessibility.Public, Outer.AssemblyName, Outer);
        
        StringProperty stringProperty = new StringProperty("Exception", Accessibility.Public, delegateFunction);
        stringProperty.MakeParameter();

        List <UnrealProperty> properties = new List<UnrealProperty>(2);
        
        if (returnValue is not VoidProperty)
        {
            properties.Add(returnValue);
            returnValue.PropertyFlags = 0;
            returnValue.SourceName = "Result";
            returnValue.MakeParameter();
        }
        
        properties.Add(stringProperty);
        
        delegateFunction.Properties = new EquatableList<UnrealProperty>(properties);
        delegateFunction.ReturnType = new VoidProperty(delegateFunction);
        
        UnrealDelegate asyncDelegate = new UnrealDelegate(delegateFunction, true);
        delegateFunction.Outer = asyncDelegate;
        
        asyncDelegate.AppendFunctionAsDelegate(builder);
        asyncDelegate.ExportType(builder, spc);
        
        builder.BeginModuleInitializer(asyncDelegate);
        builder.AppendLine();
    }
    
    void AppendWrapperVariables(GeneratorStringBuilder builder, UnrealClass asyncWrapperClass, string wrapperDelegateName, bool hasCancellationToken)
    {
        DelegateProperty completedProperty = new DelegateProperty(PropertyType.MulticastInlineDelegate, wrapperDelegateName, "Completed", Accessibility.Public, asyncWrapperClass);
        completedProperty.MakeBlueprintAssignable();
        
        DelegateProperty failedProperty = new DelegateProperty(PropertyType.MulticastInlineDelegate, wrapperDelegateName, "Failed", Accessibility.Public, asyncWrapperClass);
        failedProperty.MakeBlueprintAssignable();
        
        asyncWrapperClass.Properties = new EquatableList<UnrealProperty>([completedProperty, failedProperty]);
        
        builder.AppendLine($"private Task<{ReturnType.ManagedType}>? _task;");
        
        if (hasCancellationToken)
        {
            builder.AppendLine("private readonly CancellationTokenSource _cancellationTokenSource = new();");
        }

        builder.AppendLine($"public Func<CancellationToken, Task<{ReturnType.ManagedType}>>? asyncDelegate;");
    }

    void AppendActivateOverride(GeneratorStringBuilder builder)
    {
        builder.AppendLine("protected override void Activate_Implementation()");
        builder.OpenBrace();
        builder.AppendLine("if (asyncDelegate == null) { throw new InvalidOperationException(\"AsyncDelegate was null\"); }");
        builder.AppendLine(" _task = asyncDelegate(_cancellationTokenSource.Token);");
        builder.AppendLine(" _task.ContinueWith(OnTaskCompleted);");
        builder.CloseBrace();
    }

    void AppendCancelOverride(GeneratorStringBuilder builder, bool hasCancellationToken)
    {
        builder.AppendLine("protected override void Cancel_Implementation()");
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
        string taskTypeName = ReturnType is VoidProperty ? "Task" : $"Task<{ReturnType.ManagedType}>";
        builder.AppendLine($"void OnTaskCompleted({taskTypeName} t)");
        builder.OpenBrace();
        builder.AppendLine("if (UnrealSynchronizationContext.CurrentThread != NamedThread.GameThread)");
        builder.OpenBrace();
        builder.AppendLine("new UnrealSynchronizationContext(NamedThread.GameThread, t).Post(_ => OnTaskCompleted(t), null);");
        builder.AppendLine("return;");
        builder.CloseBrace();
        
        if (hasCancellationToken)
        {
            builder.AppendLine("if (_cancellationTokenSource.IsCancellationRequested) { return; }");
        }
        
        builder.AppendLine("if (IsDestroyed) { return; }");
        builder.AppendLine("if (t.IsFaulted)");
        builder.OpenBrace();
        builder.AppendLine("Failed?.InnerDelegate.Invoke(default!, t.Exception?.ToString() ?? \"Faulted without exception\");");
        builder.CloseBrace();
        builder.AppendLine("else");
        builder.OpenBrace();
        
        if (ReturnType is VoidProperty)
        {
            builder.AppendLine("Completed?.InnerDelegate.Invoke(null);");
        }
        else
        {
            builder.AppendLine("Completed?.InnerDelegate.Invoke(t.Result, null);");
        }
        
        builder.CloseBrace();
        builder.CloseBrace();
    }
    
    void AppendAsyncWrapperMetaData(GeneratorStringBuilder builder, List<UnrealProperty> properties, UnrealClass asyncWrapperClass, bool hasCancellationToken, SourceProductionContext spc, string wrapperName)
    {
        UnrealFunctionBase asyncFactoryFunction = new UnrealFunction(EFunctionFlags.BlueprintCallable | EFunctionFlags.Static, 
            SourceName, 
            Namespace, 
            Accessibility.Public, 
            AssemblyName,
            asyncWrapperClass);
        
        ObjectProperty returnValue = new ObjectProperty(wrapperName, SourceGenUtilities.ReturnValueName, Accessibility.NotApplicable, asyncFactoryFunction);
        returnValue.MakeReturnParameter();
        
        ObjectProperty targetParam = new ObjectProperty(Outer!.SourceName, "Target", Accessibility.NotApplicable, asyncFactoryFunction);
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
        
        string conversion = ReturnValueType == ReturnValueType.ValueTask ? ".AsTask()" : string.Empty;
        
        builder.AppendLine("public static " + wrapperName + " " + SourceName + "(" + string.Join(", ", asyncFactoryFunction.Properties.Select(p => $"{p.ManagedType} {p.SourceName}")) + ")");
        builder.OpenBrace();
        builder.AppendLine($"var action = NewObject<{wrapperName}>(Target);");
        builder.AppendLine("action.asyncDelegate = (cancellationToken) => Target." + SourceName + "(" + string.Join(", ", asyncFactoryFunction.Properties.Skip(1).Select(p => p.SourceName)) + (hasCancellationToken ? ", cancellationToken" : string.Empty) + $"){conversion};");
        builder.AppendLine("return action;");
        builder.CloseBrace();
        
        asyncFactoryFunction.ExportType(builder, spc);
        
        asyncWrapperClass.AddFunction(asyncFactoryFunction);
        asyncWrapperClass.TryExportProperties(builder, spc);
    }
}