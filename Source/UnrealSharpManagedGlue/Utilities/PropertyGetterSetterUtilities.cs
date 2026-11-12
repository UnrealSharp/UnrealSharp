using System;
using System.Collections.Generic;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Exporters;

namespace UnrealSharpManagedGlue.Utilities;

public static class PropertyGetterSetterUtilities
{
    public static bool MakeGetterSetterPair(this UhtFunction function, Dictionary<string, GetterSetterPair> getterSetterPairs,
        UhtClass? implementationClass = null)
    {
        if (!function.TryGetGetterSetterInfo(out string propertyName, out bool isGetter, out UhtFunction? counterpart))
        {
            return false;
        }

        UhtClass owningClass = (UhtClass)function.Outer!;
        
        UhtProperty? propertyWithSameName = owningClass.FindPropertyByName(propertyName, (property, name) =>
        {
            return property.SourceName == name || property.GetPropertyName() == name;
        });
        
        UhtProperty primaryProperty = GetPrimaryProperty(function);
        if (propertyWithSameName != null && (!propertyWithSameName.IsSameType(primaryProperty) || propertyWithSameName.HasAnyGetter() || propertyWithSameName.HasAnySetter()))
        {
            return false;
        }

        if (!getterSetterPairs.TryGetValue(propertyName, out GetterSetterPair? pair))
        {
            pair = new GetterSetterPair(propertyName, primaryProperty);
            getterSetterPairs[propertyName] = pair;
        }

        if (pair.Accessors.Count == 2)
        {
            return true;
        }

        if (isGetter)
        {
            pair.Getter = function;
            pair.GetterExporter = CreateAccessorExporter(function, primaryProperty, GetterSetterMode.Get, implementationClass);

            if (counterpart == null || pair.Setter != null)
            {
                return true;
            }
            
            pair.Setter = counterpart;
            pair.SetterExporter = CreateAccessorExporter(counterpart, primaryProperty, GetterSetterMode.Set, implementationClass);
        }
        else
        {
            pair.Setter = function;
            pair.SetterExporter = CreateAccessorExporter(function, primaryProperty, GetterSetterMode.Set, implementationClass);

            if (counterpart == null || pair.Getter != null)
            {
                return true;
            }
            
            pair.Getter = counterpart;
            pair.GetterExporter = CreateAccessorExporter(counterpart, primaryProperty, GetterSetterMode.Get, implementationClass);
        }

        return true;
    }

    // Describe a Get/Set function's synthesized member before checking native properties.
    // This lets NameMapper detect collisions without resolving property names recursively.
    public static bool TryGetGetterSetterInfo(this UhtFunction function, out string propertyName,
        out bool isGetter, out UhtFunction? counterpart)
    {
        string scriptName = function.GetFunctionName();
        isGetter = CheckIfGetter(scriptName, function);
        bool isSetter = CheckIfSetter(scriptName, function);
        propertyName = string.Empty;
        counterpart = null;

        if (!isGetter && !isSetter)
        {
            return false;
        }

        propertyName = scriptName.Length > 3 ? scriptName.Substring(3) : function.SourceName;
        propertyName = NameMapper.EscapeKeywords(propertyName);

        UhtClass owningClass = (UhtClass)function.Outer!;
        UhtFunction? sameNameFunction = owningClass.FindFunctionByName(propertyName);
        if (sameNameFunction != null && sameNameFunction != function)
        {
            return false;
        }

        counterpart = FindCounterpart(owningClass, propertyName, isGetter);
        return counterpart == null || CanShareProperty(isGetter ? function : counterpart, isGetter ? counterpart : function);
    }

    // Read only explicit bindings here: IsAnyPropertyGetter/Setter also infer bindings
    // from GetPropertyName, which would recurse during name-conflict detection.
    internal static bool IsDeclaredPropertyAccessor(this UhtFunction function)
    {
        if (function.HasMetadata("BlueprintGetter") || function.HasMetadata("BlueprintSetter"))
        {
            return true;
        }

        UhtClass owningClass = (UhtClass)function.Outer!;
        foreach (UhtProperty property in owningClass.Properties)
        {
            string? getter = property.Getter ?? property.GetMetadata("BlueprintGetter");
            string? setter = property.Setter ?? property.GetMetadata("BlueprintSetter");
            if (string.Equals(function.SourceName, getter, StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(function.SourceName, setter, StringComparison.InvariantCultureIgnoreCase)
                || (property.HasNativeGetter() && function.SourceName == "Get" + property.EngineName)
                || (property.HasNativeSetter() && function.SourceName == "Set" + property.EngineName))
            {
                return true;
            }
        }

        return false;
    }

    static bool CheckIfGetter(string scriptName, UhtFunction function)
    {
        if (!scriptName.StartsWith("Get", StringComparison.Ordinal) || function.IsBlueprintEvent())
        {
            return false;
        }

        int paramCount = function.Properties.Count();
        if (function.ReturnProperty != null)
        {
            return paramCount == 1 || (paramCount == 2 && function.Properties.First().IsWorldContextParameter());
        }

        return paramCount == 1 && function.HasOutParams() && !function.Properties.First().HasAnyFlags(EPropertyFlags.ConstParm);
    }

    static bool CheckIfSetter(string scriptName, UhtFunction function)
    {
        if (!scriptName.StartsWith("Set", StringComparison.Ordinal) || function.IsBlueprintEvent() || function.ReturnProperty != null)
        {
            return false;
        }

        UhtProperty? property = function.Properties.FirstOrDefault();
        return property != null && function.Properties.Count() == 1 && (!property.HasAllFlags(EPropertyFlags.OutParm | EPropertyFlags.ReferenceParm) || property.HasAllFlags(EPropertyFlags.ConstParm));
    }
    
    static UhtFunction? FindCounterpart(UhtClass owningClass, string propertyName, bool isCurrentGetter)
    {
        string counterpartName = (isCurrentGetter ? "Set" : "Get") + propertyName;
        UhtFunction? candidate = owningClass.FindFunctionByName(counterpartName, includeSuper: true);
        if (candidate == null)
        {
            return null;
        }

        string scriptName = candidate.GetFunctionName();
        bool isValid = isCurrentGetter ? CheckIfSetter(scriptName, candidate) : CheckIfGetter(scriptName, candidate);
        return isValid ? candidate : null;
    }

    // Differing types are otherwise left to managed conversions (FText -> string etc.), but two object
    // pointers only convert when the getter's class derives from the setter's. An inherited
    // UFoo* GetWidget() must not become the getter of a UBar* property made by SetWidget(UBar*).
    // Class references are left alone: TSubclassOf<UObject> converts through its own marshaller overload.
    static bool CanShareProperty(UhtFunction getter, UhtFunction setter)
    {
        if (GetPrimaryProperty(getter) is not UhtObjectProperty getterProperty || getterProperty is UhtClassProperty
            || GetPrimaryProperty(setter) is not UhtObjectProperty setterProperty || setterProperty is UhtClassProperty)
        {
            return true;
        }

        return getterProperty.Class.IsChildOf(setterProperty.Class);
    }

    static GetterSetterFunctionExporter CreateAccessorExporter(UhtFunction function, UhtProperty property, GetterSetterMode mode,
        UhtClass? implementationClass)
    {
        // Keep the interface declaration in the pair so explicit implementations use its
        // name, but marshal using the override's parameters when its names differ.
        UhtFunction? implementation = implementationClass?.FindFunctionByName(function.SourceName, includeSuper: true);
        if (implementation != null && implementation.HasSameSignature(function))
        {
            function = implementation;
        }

        return GetterSetterFunctionExporter.Create(function, property, mode, EFunctionProtectionMode.UseUFunctionProtection);
    }
    
    static UhtProperty GetPrimaryProperty(UhtFunction function)
    {
        if (function.ReturnProperty != null)
        {
            return function.ReturnProperty;
        }

        return function.Properties.FirstOrDefault(p => p.HasAllFlags(EPropertyFlags.OutParm) && !p.HasAllFlags(EPropertyFlags.ConstParm)) ?? function.Properties.First();
    }
}
