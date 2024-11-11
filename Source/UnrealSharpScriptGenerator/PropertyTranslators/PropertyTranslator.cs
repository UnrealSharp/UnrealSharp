using System;
using System.Collections.Generic;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Exporters;
using UnrealSharpScriptGenerator.Tooltip;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

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
    
    public PropertyTranslator(EPropertyUsageFlags supportedPropertyUsage)
    {
        _supportedPropertyUsage = supportedPropertyUsage;
    }
    
    // Can we export this property?
    public abstract bool CanExport(UhtProperty property);
    
    // Get the managed type for this property
    // Example: "int" for a property of type "int32"
    public abstract string GetManagedType(UhtProperty property);
    
    // Get the marshaller for this property to marshal back and forth between C++ and C#
    public abstract string GetMarshaller(UhtProperty property);
    
    // Get the marshaller delegates for this property
    public abstract string ExportMarshallerDelegates(UhtProperty property);
    
    // Get the null value for this property
    public abstract string GetNullValue(UhtProperty property);
    
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
            builder.AppendLine($"{variableDeclaration}{propertyPointerName} = {ExporterCallbacks.FPropertyCallbacks}.CallGetNativePropertyFromName(NativeClassPtr, \"{adjustedNativePropertyName}\");");
            builder.AppendLine($"{nativePropertyName}_Offset = {ExporterCallbacks.FPropertyCallbacks}.CallGetPropertyOffset({propertyPointerName});");
        }
        
        if (hasNativeGetterSetter)
        {
            builder.AppendLine($"{nativePropertyName}_Size = {ExporterCallbacks.FPropertyCallbacks}.CallGetSize({propertyPointerName});");
        }
        
        // Export the static constructors for the getter and setter
        TryExportGetterSetterStaticConstructor(property, builder);
    }
    
    private void TryExportGetterSetterStaticConstructor(UhtProperty property, GeneratorStringBuilder builder)
    {
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
        builder.AppendLine($"{functionName}_{propertyEngineName}_Offset = {ExporterCallbacks.FPropertyCallbacks}.CallGetPropertyOffsetFromName({functionName}_NativeFunction, \"{propertyEngineName}\");");
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
        builder.AppendLine($"static int {nativeMethodName}_{propertyEngineName}_Offset;");
    }

    public virtual void ExportPropertyGetter(GeneratorStringBuilder builder, UhtProperty property, string propertyManagedName)
    {
        ExportFromNative(builder, property, propertyManagedName, "return", "NativeObject", $"{propertyManagedName}_Offset", false, false);
    }

    public virtual void ExportPropertySetter(GeneratorStringBuilder builder, UhtProperty property, string propertyManagedName)
    {
        ExportToNative(builder, property, propertyManagedName, "NativeObject", $"{propertyManagedName}_Offset", "value");
    }

    public virtual void ExportCppDefaultParameterAsLocalVariable(GeneratorStringBuilder builder, string variableName,
        string defaultValue, UhtFunction function, UhtProperty paramProperty)
    {
        
    }

    public virtual void ExportFunctionReturnStatement(GeneratorStringBuilder builder,
        UhtProperty property,
        string nativePropertyName, 
        string functionName, 
        string paramsCallString)
    {
        throw new NotImplementedException();
    }
    
    // Cleanup the marshalling buffer
    public virtual void ExportCleanupMarshallingBuffer(GeneratorStringBuilder builder, UhtProperty property,
        string paramName)
    {

    }
    
    // Build the C# code to marshal this property from C++ to C#
    public abstract void ExportFromNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName,
        string assignmentOrReturn, string sourceBuffer, string offset, bool bCleanupSourceBuffer,
        bool reuseRefMarshallers);
    
    // Build the C# code to marshal this property from C# to C++
    public abstract void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer, string offset, string source);
    
    // Convert a C++ default value to a C# default value
    // Example: "0.0f" for a float property
    public abstract string ConvertCPPDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter);

    public void ExportCustomProperty(GeneratorStringBuilder builder, GetterSetterPair getterSetterPair, string propertyName, UhtProperty property)
    {
        GetterSetterFunctionExporter? getterExporter = getterSetterPair.GetterExporter;
        GetterSetterFunctionExporter? setterExporter = getterSetterPair.SetterExporter;
        
        void ExportBackingFields()
        {
            if (getterExporter != null)
            {
                getterExporter.ExportFunctionVariables(builder);
            }
            
            if (setterExporter != null)
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
        
        ExportProperty_Internal(builder, property, propertyName, ExportBackingFields, ExportProtection, exportGetterAction, exportSetterAction);
    }
    
    public void ExportGetSetProperty(GeneratorStringBuilder builder, GetterSetterPair getterSetterPair, UhtProperty property, Dictionary<UhtFunction, FunctionExporter> exportedGetterSetters)
    {
        void ExportNativeGetter()
        {
            builder.BeginUnsafeBlock();
            builder.AppendStackAllocProperty($"{property.SourceName}_Size", property.GetNativePropertyName());
            builder.AppendLine($"FPropertyExporter.CallGetValue_InContainer({property.GetNativePropertyName()}, NativeObject, ParamsBuffer);");
            ExportFromNative(builder, property, property.SourceName, $"{GetManagedType(property)} newValue =", "ParamsBuffer", "0", false, false);
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
            ExportToNative(builder, property, property.SourceName, "ParamsBuffer", "0", "value");
            builder.AppendLine($"FPropertyExporter.CallSetValue_InContainer({property.GetNativePropertyName()}, NativeObject, ParamsBuffer);"); 
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
        else if (SupportsSetter && property.HasReadWriteAccess())
        {
            exportSetterAction = ExportSetter;
        }
        
        void ExportBackingFields()
        {
            if (getterSetterPair.GetterExporter is not null && !exportedGetterSetters.ContainsKey(getterSetterPair.Getter!))
            {
                getterSetterPair.GetterExporter.ExportFunctionVariables(builder);
                exportedGetterSetters.Add(getterSetterPair.Getter!, getterSetterPair.GetterExporter);
            }   
            
            if (getterSetterPair.SetterExporter is not null && !exportedGetterSetters.ContainsKey(getterSetterPair.Setter!))
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
        
        Action? exportSetterAction = SupportsSetter && property.HasReadWriteAccess() ? ExportSetter : null;
        ExportProperty_Internal(builder, property, property.GetPropertyName(), ExportBackingFields, null, ExportGetter, exportSetterAction); 
    }
    
    private void ExportProperty_Internal(GeneratorStringBuilder builder, UhtProperty property, string propertyName, 
        Action? backingFieldsExport,
        Func<string>? exportProtection,
        Action? exportGetter, 
        Action? exportSetter)
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
        builder.AppendLine($"{protection}{managedType} {propertyName}");
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
            builder.AppendLine("set");
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
        builder.BeginUnsafeBlock();
        exportedFunction.ExportInvoke(builder);
        builder.EndUnsafeBlock();
    }

    public void ExportMirrorProperty(GeneratorStringBuilder builder, UhtProperty property, bool suppressOffsets, List<string> reservedNames)
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
        builder.AppendTooltip(property);
        builder.AppendLine($"{protection}{managedType} {propertyScriptName};");
        builder.AppendLine();
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
}