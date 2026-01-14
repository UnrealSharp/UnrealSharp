using EpicGames.UHT.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using UnrealSharpManagedGlue.Exporters;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;
using UnrealSharpManagedGlue.Tooltip;

namespace UnrealSharpManagedGlue.PropertyTranslators;

public abstract class PropertyTranslator
{
    private readonly EPropertyUsageFlags _supportedPropertyUsage;
    protected const EPropertyUsageFlags ContainerSupportedUsages = EPropertyUsageFlags.Property
                                                                   | EPropertyUsageFlags.StructProperty
                                                                   | EPropertyUsageFlags.Parameter
                                                                   | EPropertyUsageFlags.ReturnValue;
    
    public bool IsSupportedAsProperty() => _supportedPropertyUsage.HasFlag(EPropertyUsageFlags.Property);
    public bool IsSupportedAsParameter() => _supportedPropertyUsage.HasFlag(EPropertyUsageFlags.Parameter);
    public bool IsSupportedAsReturnValue() => _supportedPropertyUsage.HasFlag(EPropertyUsageFlags.ReturnValue);
    public bool IsSupportedAsInner() => _supportedPropertyUsage.HasFlag(EPropertyUsageFlags.Inner);
    public bool IsSupportedAsStructProperty() => _supportedPropertyUsage.HasFlag(EPropertyUsageFlags.StructProperty);

    // Is this property the same memory layout as the C++ type?
    public virtual bool IsBlittable => false;
    public virtual bool SupportsSetter => true;
    public virtual bool ExportDefaultParameter => true;
    public virtual bool CacheProperty => false;

    // Should this property be declared as a parameter in the function signature? 
    // A property can support being a parameter but not be declared as one, such as WorldContextObjectPropertyTranslator
    public virtual bool ShouldBeDeclaredAsParameter => true;
    
    public PropertyTranslator(EPropertyUsageFlags supportedPropertyUsage)
    {
        _supportedPropertyUsage = supportedPropertyUsage;
    }
    
    // Can we export this property?
    public abstract bool CanExport(UhtProperty property);

    // Can we support generic types?
    public abstract bool CanSupportGenericType(UhtProperty property);
    
    // Can we support custom structs?
    public virtual bool CanSupportCustomStruct(UhtProperty property) => false;

    // Get the managed type for this property
    // Example: "int" for a property of type "int32"
    public abstract string GetManagedType(UhtProperty property);
    
    // Get the marshaller for this property to marshal back and forth between C++ and C#
    public abstract string GetMarshaller(UhtProperty property);
    
    // Get the marshaller delegates for this property
    public abstract string ExportMarshallerDelegates(UhtProperty property);
    
    // Get the null value for this property
    public virtual string GetNullValue(UhtProperty property) => $"default({GetManagedType(property)})";
    
    public virtual void ExportPropertyStaticConstructor(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        string propertyPointerName = property.GetNativePropertyName();
        bool hasNativeGetterSetter = property.HasAnyNativeGetterSetter();
        bool hasBlueprintGetterSetter = property.HasBlueprintGetterSetterPair();

        string adjustedNativePropertyName = nativePropertyName;
        if (property.Deprecated)
        {
            int index = nativePropertyName.IndexOf("_DEPRECATED", StringComparison.OrdinalIgnoreCase);
            adjustedNativePropertyName = index != -1 ? nativePropertyName.Substring(0, index) : nativePropertyName;
        }

        if (hasNativeGetterSetter || !hasBlueprintGetterSetter)
        {
            string variableDeclaration = CacheProperty || hasNativeGetterSetter ? "" : "IntPtr ";
            builder.AppendLine($"{variableDeclaration}{propertyPointerName} = CallGetNativePropertyFromName(NativeClassPtr, \"{adjustedNativePropertyName}\");");
            builder.AppendLine($"{nativePropertyName}_Offset = CallGetPropertyOffset({propertyPointerName});");
        }
        
        if (hasNativeGetterSetter)
        {
            builder.AppendLine($"{nativePropertyName}_Size = CallGetSize({propertyPointerName});");
        }
        
        if (!property.HasNativeGetter())
        {
            UhtFunction? getter = property.GetBlueprintGetter();
            if (getter != null)
            {
                StaticConstructorUtilities.ExportClassFunctionStaticConstructor(builder, getter);
            }
        }
        
        if (!property.HasNativeSetter())
        {
            UhtFunction? setter = property.GetBlueprintSetter();
            if (setter != null)
            {
                StaticConstructorUtilities.ExportClassFunctionStaticConstructor(builder, setter);
            }
        }
    }
    
    public virtual void ExportParameterStaticConstructor(GeneratorStringBuilder builder, UhtProperty property, UhtFunction function, string propertyEngineName, string functionName)
    {
        string variableName = $"{functionName}_{propertyEngineName}_{(property.GetPrecedingCustomStructParams() > 0 ? "NativeOffset" : "Offset")}";
        builder.AppendLine($"{variableName} = CallGetPropertyOffsetFromName({functionName}_NativeFunction, \"{propertyEngineName}\");");
    }
    
    public virtual void ExportPropertyVariables(GeneratorStringBuilder builder, UhtProperty property, string propertyEngineName)
    {
        builder.AppendLine($"static int {propertyEngineName}_Offset;");
        
        if (property.HasAnyGetterOrSetter() || CacheProperty)
        {
            AddNativePropertyField(builder, propertyEngineName);
        }
        
        if (property.HasAnyGetterOrSetter())
        {
            builder.AppendLine($"static int {propertyEngineName}_Size;");
        }
    }
    
    public virtual void ExportParameterVariables(GeneratorStringBuilder builder, UhtFunction function, string nativeMethodName, UhtProperty property, string propertyEngineName)
    {
        if (function.HasCustomStructParamSupport())
        {
            List<UhtProperty> precedingParams = property.GetPrecedingParams()!;
            int precedingCustomStructProperties = precedingParams.Count(param => param.IsCustomStructureType());
            if (precedingCustomStructProperties > 0)
            {
                builder.AppendLine($"static int {nativeMethodName}_{propertyEngineName}_NativeOffset;");
                List<string> customStructParamTypes = function.GetCustomStructParamTypes().GetRange(0, precedingCustomStructProperties);
                builder.AppendLine($"static int {nativeMethodName}_{propertyEngineName}_Offset<{string.Join(", ", customStructParamTypes)}>()");
                builder.Indent();
                foreach (string customStructParamType in customStructParamTypes)
                {
                    builder.AppendLine($"where {customStructParamType} : MarshalledStruct<{customStructParamType}>");
                }

                string variableNames = string.Join(" + ", customStructParamTypes.ConvertAll(customStructParamType => $"{customStructParamType}.GetNativeDataSize()"));
                string nativeOffsetSubtractionMultiplier = string.Empty;
                
                if (precedingCustomStructProperties > 1)
                {
                    nativeOffsetSubtractionMultiplier += $"{precedingCustomStructProperties} * ";
                }

                builder.AppendLine($"=> {nativeMethodName}_{propertyEngineName}_NativeOffset + {variableNames} - {nativeOffsetSubtractionMultiplier}sizeof(int);");
                builder.UnIndent();
                return;
            }
        }
        builder.AppendLine($"static int {nativeMethodName}_{propertyEngineName}_Offset;");
    }

    public virtual void ExportPropertyGetter(GeneratorStringBuilder builder, UhtProperty property, string propertyManagedName)
    {
        ExportFromNative(builder, property, propertyManagedName, "return", "NativeObject", $"{propertyManagedName}_Offset", false, false);
    }

    public virtual void ExportPropertySetter(GeneratorStringBuilder builder, UhtProperty property, string propertyManagedName)
    {
        ExportToNative(builder, property, propertyManagedName, "NativeObject", $"{propertyManagedName}_Offset", "value", false);
    }

    public virtual void ExportCppDefaultParameterAsLocalVariable(GeneratorStringBuilder builder, string variableName,
        string defaultValue, UhtFunction function, UhtProperty paramProperty)
    {
        
    }
    
    // Cleanup the marshalling buffer
    public virtual void ExportCleanupMarshallingBuffer(GeneratorStringBuilder builder, UhtProperty property,
        string paramName)
    {

    }
    
    // Build the C# code to marshal this property from C++ to C#
    public abstract void ExportFromNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName,
        string assignmentOrReturn, string sourceBuffer, string offset, bool cleanupSourceBuffer,
        bool reuseRefMarshallers);
    
    // Build the C# code to marshal this property from C# to C++
    public abstract void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName,
        string destinationBuffer, string offset, string source, bool reuseRefMarshallers);
    
    // Convert a C++ default value to a C# default value
    // Example: "0.0f" for a float property
    public abstract string ConvertCppDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter);

    public void ExportCustomProperty(GeneratorStringBuilder builder, GetterSetterPair getterSetterPair, 
                                     string propertyName, UhtProperty property, bool isExplicitImplementation = false,
                                     HashSet<string>? exportedFunctionNames = null)
    {
        GetterSetterFunctionExporter? getterExporter = getterSetterPair.GetterExporter;
        GetterSetterFunctionExporter? setterExporter = getterSetterPair.SetterExporter;
        
        void ExportBackingFields()
        {
            if (getterExporter != null && (exportedFunctionNames is null || !exportedFunctionNames.Contains(getterExporter.Function.SourceName)))
            {
                getterExporter.ExportFunctionVariables(builder);
            }
            
            if (setterExporter != null && (exportedFunctionNames is null || !exportedFunctionNames.Contains(setterExporter.Function.SourceName)))
            {
                setterExporter.ExportFunctionVariables(builder);
            }
        }
        
        string ExportProtection()
        {
            if (setterExporter != null)
            {
                return setterExporter.Modifiers;
            }
            
            if (getterExporter != null)
            {
                return getterExporter.Modifiers;
            }
            
            throw new InvalidOperationException("No getter or setter found");
        }
        
        Action? exportGetterAction = getterExporter != null ? () => AppendInvoke(builder, getterExporter) : null;
        Action? exportSetterAction = setterExporter != null ? () => AppendInvoke(builder, setterExporter) : null;
        
        string? interfaceName = isExplicitImplementation ? getterSetterPair.Accessors.First().Outer?.GetFullManagedName() : null;
        
        ExportProperty_Internal(builder, property, propertyName, ExportBackingFields, ExportProtection, exportGetterAction, exportSetterAction, interfaceName: interfaceName);
    }
    
    public void ExportGetSetProperty(GeneratorStringBuilder builder, GetterSetterPair getterSetterPair, UhtProperty property, 
                                     Dictionary<UhtFunction, FunctionExporter> exportedGetterSetters,
                                     HashSet<string>? exportedFunctionName = null)
    {
        void ExportNativeGetter()
        {
            builder.BeginUnsafeBlock();
            builder.AppendStackAllocProperty($"{property.SourceName}_Size", property.GetNativePropertyName());
            builder.AppendLine($"CallGetValue_InContainer({property.GetNativePropertyName()}, NativeObject, paramsBuffer);");
            ExportFromNative(builder, property, property.SourceName, $"{GetManagedType(property)} newValue =", "paramsBuffer", "0", true, false);
            builder.AppendLine("return newValue;");
            builder.EndUnsafeBlock();
        }
        
        void ExportBlueprintGetter()
        {
            AppendInvoke(builder, getterSetterPair.GetterExporter!);
        }
        
        void ExportGetter()
        {
            ExportPropertyGetter(builder, property, property.SourceName);
        }
        
        void ExportNativeSetter()
        {
            builder.BeginUnsafeBlock();
            builder.AppendStackAllocProperty($"{property.SourceName}_Size", property.GetNativePropertyName());
            ExportToNative(builder, property, property.SourceName, "paramsBuffer", "0", "value", false);
            builder.AppendLine($"CallSetValue_InContainer({property.GetNativePropertyName()}, NativeObject, paramsBuffer);"); 
            ExportCleanupMarshallingBuffer(builder, property, property.SourceName);
            builder.EndUnsafeBlock();
        }
        
        void ExportBlueprintSetter()
        {
            AppendInvoke(builder, getterSetterPair.SetterExporter!);
        }
        
        void ExportSetter()
        {
            ExportPropertySetter(builder, property, property.SourceName);
        }
        
        Action? exportGetterAction;
        if (property.HasNativeGetter())
        {
            exportGetterAction = ExportNativeGetter;
        }
        else if (getterSetterPair.GetterExporter != null)
        {
            exportGetterAction = ExportBlueprintGetter;
        }
        else
        {
            exportGetterAction = ExportGetter;
        }
        
        Action? exportSetterAction = null;
        if (property.HasNativeSetter())
        {
            exportSetterAction = ExportNativeSetter;
        }
        else if (getterSetterPair.SetterExporter != null)
        {
            exportSetterAction = ExportBlueprintSetter;
        }
        else if (SupportsSetter && property.IsReadWrite())
        {
            exportSetterAction = ExportSetter;
        }
        
        void ExportBackingFields()
        {
            if (getterSetterPair.GetterExporter is not null && !exportedGetterSetters.ContainsKey(getterSetterPair.Getter!) && (exportedFunctionName is null || !exportedFunctionName.Contains(getterSetterPair.Getter!.SourceName)))
            {
                getterSetterPair.GetterExporter.ExportFunctionVariables(builder);
                exportedGetterSetters.Add(getterSetterPair.Getter!, getterSetterPair.GetterExporter);
            }   
            
            if (getterSetterPair.SetterExporter is not null && !exportedGetterSetters.ContainsKey(getterSetterPair.Setter!) && (exportedFunctionName is null || !exportedFunctionName.Contains(getterSetterPair.Setter!.SourceName)))
            {
                getterSetterPair.SetterExporter.ExportFunctionVariables(builder);
                exportedGetterSetters.Add(getterSetterPair.Setter!, getterSetterPair.SetterExporter);
            }

            if (getterSetterPair.GetterExporter is null || getterSetterPair.SetterExporter is null)
            {
                ExportPropertyVariables(builder, property, property.SourceName);
            }
        }
        
        ExportProperty_Internal(builder, property, getterSetterPair.PropertyName, ExportBackingFields, null, exportGetterAction, exportSetterAction); 
    }
    
    public void ExportProperty(GeneratorStringBuilder builder, UhtProperty property)
    {
        void ExportGetter()
        {
            ExportPropertyGetter(builder, property, property.SourceName);  
        }
        
        void ExportSetter()
        {
            ExportPropertySetter(builder, property, property.SourceName);
        }
        
        void ExportBackingFields()
        {
            ExportPropertyVariables(builder, property, property.SourceName);
        }
        
        bool isReadWrite = property.IsReadWrite();
        bool isEditDefaultsOnly = property.IsEditDefaultsOnly();

        Action? exportSetterAction = SupportsSetter && (isReadWrite || isEditDefaultsOnly) ? ExportSetter : null;
        string setterOperation = isEditDefaultsOnly && !isReadWrite ? "init" : "set";

        ExportProperty_Internal(builder, property, property.GetPropertyName(), ExportBackingFields, null, ExportGetter, exportSetterAction, setterOperation); 
    }
    
    private void ExportProperty_Internal(GeneratorStringBuilder builder, UhtProperty property, string propertyName, 
        Action? backingFieldsExport,
        Func<string>? exportProtection,
        Action? exportGetter, 
        Action? exportSetter,
        string setterOperation = "set",
        string? interfaceName = null)
    {
        builder.AppendLine();
        builder.TryAddWithEditor(property);
        
        if (backingFieldsExport is not null)
        {
            backingFieldsExport();
            builder.AppendLine();
        }
        
        string protection = exportProtection != null ? exportProtection.Invoke() : property.GetProtection();
        builder.AppendTooltip(property);
        
        string managedType = GetManagedType(property);
        if (interfaceName is not null)
        {
            builder.AppendLine($"{managedType} {interfaceName}.{propertyName}");
        }
        else
        {
            builder.AppendLine($"{protection}{managedType} {propertyName}");
        }

        builder.OpenBrace();

        if (exportGetter is not null)
        {
            builder.AppendLine("get");
            builder.OpenBrace();
            exportGetter();
            builder.CloseBrace();
        }

        if (exportSetter is not null)
        {
            builder.AppendLine(setterOperation);
            builder.OpenBrace();
            exportSetter();
            builder.CloseBrace();
        }
        
        builder.CloseBrace();
        builder.TryEndWithEditor(property);
        builder.AppendLine();
    }
    
    private void AppendInvoke(GeneratorStringBuilder builder, FunctionExporter exportedFunction)
    {
        exportedFunction.ExportInvoke(builder);
    }

    public void ExportMirrorProperty(UhtStruct structObj, GeneratorStringBuilder builder, UhtProperty property, bool suppressOffsets, List<string> reservedNames, bool isReadOnly, bool useProperties)
    {
        string propertyScriptName = property.GetPropertyName();
        
        builder.AppendLine($"// {propertyScriptName}");
        builder.AppendLine();
        
        if (!suppressOffsets)
        {
            ExportPropertyVariables(builder, property, property.SourceName);
        }
        
        string protection = property.GetProtection();
        string managedType = GetManagedType(property);
        string required = property.HasMetadata("Required") ? "required " : "";;
        builder.AppendTooltip(property);
        if (structObj.IsStructNativelyCopyable())
        {
            
            builder.AppendLine($"{protection}{required}{managedType} {propertyScriptName}");
            builder.OpenBrace();
            builder.AppendLine("get");
            builder.OpenBrace();
            GenerateMirrorPropertyBody(structObj, builder, property, true);
            builder.CloseBrace();
            
            builder.AppendLine(isReadOnly ? "init" : "set");
            builder.OpenBrace();
            GenerateMirrorPropertyBody(structObj, builder, property, false);
            builder.CloseBrace();
            builder.CloseBrace();
        }
        else if (useProperties)
        {
            string setter = isReadOnly ? "init;" : "set;";
            builder.AppendLine($"{protection}{required}{managedType} {propertyScriptName} {{ get; {setter} }}");
        }
        else
        {
            string @readonly = isReadOnly ? "readonly " : "";
            builder.AppendLine($"{protection}{required}{@readonly}{managedType} {propertyScriptName};");
        }
        builder.AppendLine();
    }

    private void GenerateMirrorPropertyBody(UhtStruct structObj, GeneratorStringBuilder builder, UhtProperty property, bool isGetter)
    {
        bool isDestructible = structObj.IsStructNativelyDestructible();
        builder.BeginUnsafeBlock();
        if (isDestructible)
        {
            builder.AppendLine("if (NativeHandle is null)");
            builder.OpenBrace();
            builder.AppendLine("NativeHandle = new NativeStructHandle(NativeClassPtr);");
        }
        else
        {
            builder.AppendLine("if (Allocation is null)");
            builder.OpenBrace();
            builder.AppendLine("Allocation = new byte[NativeDataSize];");
        }

        builder.CloseBrace();
        builder.AppendLine();

        if (isDestructible)
        {
            builder.AppendLine("fixed (NativeStructHandleData* StructDataPointer = &NativeHandle.Data)");
            builder.OpenBrace();
            builder.AppendLine($"IntPtr AllocationPointer = {ExporterCallbacks.UScriptStructCallbacks}.CallGetStructLocation(StructDataPointer, NativeClassPtr);");
        }
        else
        {
            builder.AppendLine("fixed (byte* AllocationPointer = Allocation)");
            builder.OpenBrace();
        }

        if (isGetter)
        {
            ExportFromNative(builder, property, property.SourceName, "return", "(IntPtr) AllocationPointer", $"{property.SourceName}_Offset", false, false);
        }
        else
        {
            ExportToNative(builder, property, property.SourceName, "(IntPtr) AllocationPointer", $"{property.SourceName}_Offset", "value", false);
        }
        
        builder.CloseBrace();
        builder.CloseBrace();
    }

    public string GetCppDefaultValue(UhtFunction function, UhtProperty parameter)
    {
        string metaDataKey = $"CPP_Default_{parameter.SourceName}";
        return function.GetMetadata(metaDataKey);
    }
    
    protected void AddNativePropertyField(GeneratorStringBuilder builder, string propertyName)
    {
        builder.AppendLine($"static IntPtr {GetNativePropertyField(propertyName)};");
    }

    protected string GetNativePropertyField(string propertyName)
    {
        return $"{propertyName}_NativeProperty";
    }

    public void ExportPropertyArithmetic(GeneratorStringBuilder stringBuilder, UhtProperty property, ArithmeticKind arithmeticKind)
    {
        string scriptName = property.GetScriptName();

        static string Bin(string op, string lhs, string rhs) => $"{lhs} {op} {rhs}";

        string rhsExpr = arithmeticKind switch
        {
            ArithmeticKind.Add => Bin("+", $"lhs.{scriptName}", $"rhs.{scriptName}"),
            ArithmeticKind.Subtract => Bin("-", $"lhs.{scriptName}", $"rhs.{scriptName}"),
            ArithmeticKind.Multiply => Bin("*", $"lhs.{scriptName}", $"rhs.{scriptName}"),
            ArithmeticKind.Divide => Bin("/", $"lhs.{scriptName}", $"rhs.{scriptName}"),
            ArithmeticKind.Modulo => Bin("%", $"lhs.{scriptName}", $"rhs.{scriptName}"),
            _ => throw new NotSupportedException($"Arithmetic kind {arithmeticKind} not supported.")
        };

        string managedType = GetManagedType(property);

        stringBuilder.Append($"{scriptName} = ({managedType})({rhsExpr})");
    }
}