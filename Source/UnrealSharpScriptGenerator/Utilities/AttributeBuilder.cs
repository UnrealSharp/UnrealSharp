using System;
using System.Text;
using EpicGames.Core;
using EpicGames.UHT.Types;

namespace UnrealSharpScriptGenerator.Utilities;

public class AttributeBuilder
{
    private readonly StringBuilder _stringBuilder;
    private AttributeState _state;
    
    private AttributeBuilder()
    {
        _stringBuilder = new StringBuilder("[");
        _state = AttributeState.Open;
    }

    public static AttributeBuilder CreateAttributeBuilder(UhtType type)
    {
        AttributeBuilder builder = new();
        builder.AddAttribute(GetAttributeForType(type));
        return builder;
    }

    public void AddGeneratedTypeAttribute(UhtType type)
    {
        AddAttribute("GeneratedType");
        AddArgument($"\"{type.EngineName}\"");
        
        if (type.EngineType is UhtEngineType.Enum or UhtEngineType.ScriptStruct or UhtEngineType.Class)
        {
            string fullName = type.GetNamespace() + "." + type.EngineName;
            AddArgument($"\"{fullName}\"");
        }
    }
    
    public void AddIsBlittableAttribute()
    {
        AddAttribute("BlittableType");
    }

    private static string GetAttributeForType(UhtType type)
    {
        if (type is UhtClass uhtClass)
        {
            return uhtClass.HasAllFlags(EClassFlags.Interface) ? "UInterface" : "UClass";
        }

        if (type is UhtScriptStruct)
        {
            return "UStruct";
        }

        if (type is UhtEnum)
        {
            return "UEnum";
        }

        if (type is UhtFunction)
        {
            return "UFunction";
        }

        throw new InvalidOperationException("Invalid type");
    }

    public void AddAttribute(string attributeName)
    {
        switch (_state)
        {
            case AttributeState.Open:
                break;
            case AttributeState.InAttribute:
                _stringBuilder.Append(", ");
                break;
            case AttributeState.InAttributeParams:
                _stringBuilder.Append("), ");
                break;
            default:
                throw new InvalidOperationException("Invalid state");
        }
        _stringBuilder.Append(attributeName);
        _state = AttributeState.InAttribute;
    }

    public void AddArgument(string arg)
    {
        switch (_state)
        {
            case AttributeState.InAttribute:
                _stringBuilder.Append("(");
                break;
            case AttributeState.InAttributeParams:
                _stringBuilder.Append(", ");
                break;
            default:
                throw new InvalidOperationException("Invalid state");
        }
        _stringBuilder.Append(arg);
        _state = AttributeState.InAttributeParams;
    }

    public void Finish()
    {
        switch (_state)
        {
            case AttributeState.InAttribute:
                _stringBuilder.Append("]");
                break;
            case AttributeState.InAttributeParams:
                _stringBuilder.Append(")]");
                break;
            default:
                throw new InvalidOperationException("Invalid state");
        }

        _state = AttributeState.Closed;
    }

    public override string ToString()
    {
        if (_state != AttributeState.Closed)
        {
            throw new InvalidOperationException("Cannot convert to string. The builder is not in the closed state.");
        }
        return _stringBuilder.ToString();
    }

    private enum AttributeState
    {
        Open,
        Closed,
        InAttribute,
        InAttributeParams
    }
}
