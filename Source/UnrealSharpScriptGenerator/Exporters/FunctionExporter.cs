using System.Collections.Generic;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.PropertyTranslators;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.Exporters;

public enum EFunctionProtectionMode
{
    UseUFunctionProtection,
    OverrideWithInternal,
    OverrideWithProtected,
}

public enum EOverloadMode
{
    AllowOverloads,
    SuppressOverloads,
};

public enum EBlueprintVisibility
{
    Call,
    Event,
};

public class FunctionExporter
{
    private UhtFunction Function;
    private string FunctionName;
    private List<PropertyTranslator> ParameterTranslators;
    private PropertyTranslator ReturnValueTranslator;
    private EOverloadMode OverloadMode;
    private EFunctionProtectionMode ProtectionMode;
    private EBlueprintVisibility BlueprintVisibility;

    private bool Protected;
    private bool BlueprintEvent => BlueprintVisibility == EBlueprintVisibility.Event;
    private string Modifiers;
    private string InvokeFunction;
    private string InvokeFirstArgument;

    private string CustomInvoke;

    private UhtProperty? SelfParameter;
    
    public FunctionExporter(UhtFunction function, EOverloadMode overloadMode, EFunctionProtectionMode protectionMode, EBlueprintVisibility blueprintVisibility)
    {
        Function = function;
        FunctionName = function.SourceName;
        OverloadMode = overloadMode;
        ProtectionMode = protectionMode;
        BlueprintVisibility = blueprintVisibility;

        if (function.ReturnProperty != null)
        {
            ReturnValueTranslator = PropertyTranslatorManager.GetTranslator(function.ReturnProperty);
        }
        
        ParameterTranslators = new List<PropertyTranslator>();
        foreach (UhtProperty parameter in function.Properties)
        {
            PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(parameter);
            ParameterTranslators.Add(translator);
        }

        if (function.EngineType != UhtEngineType.Delegate)
        {
            string outerClassName = ScriptGeneratorUtilities.GetCleanTypeName(function.Outer!);
            if (function.SourceName == outerClassName)
            {
                FunctionName = "Call" + FunctionName;
            }
        }
        
        Protected = false;
        DetermineProtectionMode();

        if (function.HasAllFlags(EFunctionFlags.Static))
        {
            Modifiers += "static ";
            InvokeFunction = $"{ExporterCallbacks.UObjectCallbacks}.CallInvokeNativeStaticFunction";
        }
        else if (function.HasAllFlags(EFunctionFlags.Delegate))
        {
            if (function.HasParameters)
            {
                CustomInvoke = "ProcessDelegate(IntPtr.Zero);";
            }
            else
            {
                CustomInvoke = "ProcessDelegate(ParamsBuffer);";
            }
        }
        else
        {
            if (BlueprintEvent)
            {
                Modifiers += "virtual ";
            }
		
            if (function.IsInterfaceFunction())
            {
                Modifiers = "public ";
            }

            InvokeFunction = $"{ExporterCallbacks.UObjectCallbacks}.CallInvokeNativeFunction";
            InvokeFirstArgument = "NativeObject";
        }

        string ParamString = "";
        bool HasDefaultParameters = false;

        if (SelfParameter != null)
        {
            
        }
    }

    void DetermineProtectionMode()
    {
        switch (ProtectionMode)
        {
            case EFunctionProtectionMode.UseUFunctionProtection:
                if (Function.HasAllFlags(EFunctionFlags.Public))
                {
                    Modifiers = "public ";
                }
                else if (Function.HasAllFlags(EFunctionFlags.Protected) || Function.HasMetaData("BlueprintProtected"))
                {
                    Modifiers = "protected ";
                    Protected = true;
                }
                else
                {
                    Modifiers = "public ";
                }
                break;
            case EFunctionProtectionMode.OverrideWithInternal:
                Modifiers = "internal ";
                break;
            case EFunctionProtectionMode.OverrideWithProtected:
                Modifiers = "protected ";
                break;
        }
    }
}