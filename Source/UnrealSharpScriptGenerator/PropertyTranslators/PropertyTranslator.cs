using System;
using System.Collections.Generic;
using System.Linq;
using EpicGames.Core;
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
    public virtual bool NeedSetter => true;
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
    
    // Export the static constructor for this property
    public virtual void ExportPropertyStaticConstructor(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        string propertyPointerName = property.GetNativePropertyName();
        string variableDeclaration = CacheProperty || property.HasAnyNativeGetterSetter() ? "" : "IntPtr ";
        builder.AppendLine($"{variableDeclaration}{propertyPointerName} = {ExporterCallbacks.FPropertyCallbacks}.CallGetNativePropertyFromName(NativeClassPtr, \"{nativePropertyName}\");"); 
        builder.AppendLine($"{nativePropertyName}_Offset = {ExporterCallbacks.FPropertyCallbacks}.CallGetPropertyOffset({propertyPointerName});");
        
        if (property.HasAnyNativeGetterSetter())
        {
            builder.AppendLine($"{nativePropertyName}_Size = {ExporterCallbacks.FPropertyCallbacks}.CallGetSize({propertyPointerName});");
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
    
    /*public void ExportGetterSetterPair(GeneratorStringBuilder builder, GetterSetterPair pair)
    {
        Action? exportBackingFields = null;
        Action? exportGetter = null;
        if (pair.Getter is not null)
        {
            void ExportGetter()
            {
                AppendBlueprintGetterCall(builder, pair.Property, pair.Getter);
            }
            
            exportGetter = ExportGetter;
        }
        else if (pair.Setter is not null)
        {
            void ExportGetter()
            {
                ExportGetter_Internal(pair.Property, builder, pair.PropertyName);
            }
            
            void ExportBackingFields()
            {
                ExportPropertyVariables(builder, pair.Property, pair.PropertyName);
            }
            
            exportBackingFields = ExportBackingFields;
            exportGetter = ExportGetter;
        }
        
        Action? exportSetter = null;
        if (pair.Setter is not null)
        {
            void ExportSetter()
            {
                AppendBlueprintSetterCall(builder, pair.Setter);
            }
            
            exportSetter = ExportSetter;
        }
        
        ExportProperty_Internal(builder, pair.Property, pair.PropertyName, exportGetter, exportSetter, exportBackingFields);
    }*/
    
    public void ExportProperty(GeneratorStringBuilder builder, UhtProperty property, Dictionary<UhtFunction, FunctionExporter> exportedGetterSetters)
    {
        UhtFunction? getter = property.GetBlueprintGetter();
        UhtFunction? setter = property.GetBlueprintSetter();
        FunctionExporter? exportedGetter = null;
        FunctionExporter? exportedSetter = null;
        if (getter is not null && !exportedGetterSetters.TryGetValue(getter, out exportedGetter))
        {
            exportedGetter = GetterSetterFunctionExporter.Create(getter, property, GetterSetterMode.Getter);
        }
        
        if (setter is not null && !exportedGetterSetters.TryGetValue(setter, out exportedSetter))
        {
            exportedSetter = GetterSetterFunctionExporter.Create(setter, property, GetterSetterMode.Setter);
        }
        
        void ExportGetter()
        {
            if (property.HasAnyGetter())
            {
                if (property.HasNativeGetter())
                {
                    builder.BeginUnsafeBlock();
                    builder.AppendStackAlloc($"{property.SourceName}_Size", property.GetNativePropertyName());
                    builder.AppendLine($"FPropertyExporter.CallGetValue_InContainer({property.GetNativePropertyName()}, NativeObject, ParamsBuffer);");
                    ExportFromNative(builder, property, property.SourceName, $"{GetManagedType(property)} newValue =", "ParamsBuffer", "0", true, false);
                    builder.AppendLine("return newValue;");
                    builder.EndUnsafeBlock();
                }
                else
                {
                    AppendInvoke(builder, exportedGetter); 
                }
            }
            else
            {
                ExportPropertyGetter(builder, property, property.SourceName);  
            }
        }
        
        void ExportSetter()
        {
            if (property.HasAnySetter())
            {
                if (property.HasNativeSetter())
                {
                    builder.BeginUnsafeBlock();
                    builder.AppendStackAlloc($"{property.SourceName}_Size", property.GetNativePropertyName());
                    ExportToNative(builder, property, property.SourceName, "ParamsBuffer", "0", "value");
                    builder.AppendLine($"FPropertyExporter.CallSetValue_InContainer({property.GetNativePropertyName()}, NativeObject, ParamsBuffer);"); 
                    builder.EndUnsafeBlock();
                }
                else
                {
                    AppendInvoke(builder, exportedSetter);
                }
            }
            else
            {
                ExportPropertySetter(builder, property, property.SourceName);
            }
        }
        
        void ExportBackingFields()
        {
            if (exportedGetter is not null && !exportedGetterSetters.ContainsKey(getter))
            {
                exportedGetter.ExportFunctionVariables(builder);
                exportedGetterSetters.Add(getter, exportedGetter);
            }
            
            if (exportedSetter is not null && !exportedGetterSetters.ContainsKey(setter))
            {
                exportedSetter.ExportFunctionVariables(builder);
                exportedGetterSetters.Add(setter, exportedSetter);
            }

            ExportPropertyVariables(builder, property, property.SourceName);
        }
        
        Action? exportSetterAction = NeedSetter && property.HasReadWriteAccess() ? ExportSetter : null;
        ExportProperty_Internal(builder, property, property.GetPropertyName(), ExportGetter, exportSetterAction, ExportBackingFields); 
    }
    
    private void ExportProperty_Internal(GeneratorStringBuilder builder, UhtProperty property, string propertyName, 
        Action? exportGetter, 
        Action? exportSetter,
        Action? backingFieldsExport = null)
    {
        builder.AppendLine();
        builder.TryAddWithEditor(property);
        
        if (backingFieldsExport is not null)
        {
            backingFieldsExport();
            builder.AppendLine();
        }
        
        string protection = property.GetProtection();
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