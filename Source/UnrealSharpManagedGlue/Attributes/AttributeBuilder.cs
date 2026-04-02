using System;
using System.Text;
using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.Attributes;

public class AttributeBuilder
{
    private readonly StringBuilder _stringBuilder;
    private AttributeState _state;
    
    public AttributeBuilder()
    {
        _stringBuilder = new StringBuilder("[");
        _state = AttributeState.Open;
    }
    
    public AttributeBuilder(UhtType type) : this()
    {
        AddAttribute(GetAttributeForType(type));
    }
    
    public void AddIsBlittableAttribute()
    {
        AddAttribute("BlittableType");
    }

    public void AddStructLayoutAttribute(System.Runtime.InteropServices.LayoutKind layoutKind)
    {
        AddAttribute("StructLayout");
        AddArgument($"LayoutKind.{layoutKind}");
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
