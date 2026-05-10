using System;
using System.Collections.Generic;
using System.Linq;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Exporters;

namespace UnrealSharpManagedGlue.Utilities;

public static class PropertyGetterSetterUtilities
{
    public static bool MakeGetterSetterPair(this UhtFunction function, Dictionary<string, GetterSetterPair> getterSetterPairs)
    {
        string scriptName = function.GetFunctionName();
        bool isGetter = CheckIfGetter(scriptName, function);
        bool isSetter = CheckIfSetter(scriptName, function);

        if (!isGetter && !isSetter)
        {
            return false;
        }

        string propertyName = scriptName.Length > 3 ? scriptName.Substring(3) : function.SourceName;
        propertyName = NameMapper.EscapeKeywords(propertyName);

        UhtClass owningClass = (UhtClass)function.Outer!;

        UhtFunction? sameNameFunction = owningClass.FindFunctionByName(propertyName);
        if (sameNameFunction != null && sameNameFunction != function)
        {
            return false;
        }
        
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
        
        UhtFunction? counterpart = FindCounterpart(owningClass, propertyName, isGetter);

        if (isGetter)
        {
            pair.Getter = function;
            pair.GetterExporter = CreateAccessorExporter(function, primaryProperty, GetterSetterMode.Get);

            if (counterpart == null || pair.Setter != null)
            {
                return true;
            }
            
            pair.Setter = counterpart;
            pair.SetterExporter = CreateAccessorExporter(counterpart, primaryProperty, GetterSetterMode.Set);
        }
        else
        {
            pair.Setter = function;
            pair.SetterExporter = CreateAccessorExporter(function, primaryProperty, GetterSetterMode.Set);

            if (counterpart == null || pair.Getter != null)
            {
                return true;
            }
            
            pair.Getter = counterpart;
            pair.GetterExporter = CreateAccessorExporter(counterpart, primaryProperty, GetterSetterMode.Get);
        }

        return true;
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
    
    static GetterSetterFunctionExporter CreateAccessorExporter(UhtFunction function, UhtProperty property, GetterSetterMode mode)
        => GetterSetterFunctionExporter.Create(function, property, mode, EFunctionProtectionMode.UseUFunctionProtection);
    
    static UhtProperty GetPrimaryProperty(UhtFunction function)
    {
        if (function.ReturnProperty != null)
        {
            return function.ReturnProperty;
        }

        return function.Properties.FirstOrDefault(p => p.HasAllFlags(EPropertyFlags.OutParm) && !p.HasAllFlags(EPropertyFlags.ConstParm)) ?? function.Properties.First();
    }
}