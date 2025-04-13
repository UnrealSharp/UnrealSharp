﻿using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.NativeTypes;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.MetaData;

public class PropertyMetaData : BaseMetaData
{
    public PropertyFlags PropertyFlags { get; set; }
    public NativeDataType PropertyDataType { get; set; }
    public string RepNotifyFunctionName { get; set; }
    public LifetimeCondition LifetimeCondition { get; set; }
    public string BlueprintSetter { get; set; }
    public string BlueprintGetter { get; set; }

    // Non-serialized for JSON
    public FieldDefinition PropertyOffsetField;
    public FieldDefinition? NativePropertyField;
    public readonly MemberReference MemberRef;
    public bool IsOutParameter => (PropertyFlags & PropertyFlags.OutParm) == PropertyFlags.OutParm;
    public bool IsReferenceParameter => (PropertyFlags & PropertyFlags.ReferenceParm) == PropertyFlags.ReferenceParm;
    public bool IsReturnParameter => (PropertyFlags & PropertyFlags.ReturnParm) == PropertyFlags.ReturnParm;
    public bool IsInstancedReference => (PropertyFlags & PropertyFlags.InstancedReference) == PropertyFlags.InstancedReference;
    // End non-serialized
    
    private PropertyMetaData(MemberReference memberRef) : base(memberRef, PropertyUtilities.UPropertyAttribute)
    {
        
    }

    private PropertyMetaData(TypeReference typeRef, string paramName, ParameterType modifier) : this(typeRef)
    {
        MemberRef = typeRef;
        Name = paramName;
        PropertyDataType = typeRef.GetDataType(paramName, null);
        
        PropertyFlags flags = PropertyFlags.None;
        
        if (modifier != ParameterType.None)
        {
            flags |= PropertyFlags.Parm;
        }
        
        switch (modifier)
        {
            case ParameterType.Out:
                flags |= PropertyFlags.OutParm;
                break;
            case ParameterType.Ref:
                flags |= PropertyFlags.OutParm | PropertyFlags.ReferenceParm;
                break;
            case ParameterType.ReturnValue:
                flags |= PropertyFlags.ReturnParm | PropertyFlags.OutParm;
                break;
        }

        PropertyFlags = flags;
    }

    public PropertyMetaData(PropertyDefinition property) : this((MemberReference) property)
    {
        MemberRef = property;
        
        MethodDefinition getter = property.GetMethod;
        MethodDefinition setter = property.SetMethod;

        if (getter == null)
        {
            throw new InvalidPropertyException(property, "Unreal properties must have a default get method");
        }
        
        if (!getter.MethodIsCompilerGenerated())
        {
            throw new InvalidPropertyException(property, "Getter can not have a body for Unreal properties");
        }

        if (setter != null && !getter.MethodIsCompilerGenerated())
        {
            throw new InvalidPropertyException(property, "Setter can not have a body for Unreal properties");
        }
        
        if (getter.IsPrivate && PropertyFlags.HasFlag(PropertyFlags.BlueprintVisible))
        {
            if(!GetBoolMetadata("AllowPrivateAccess"))
            {
                throw new InvalidPropertyException(property, "Blueprint visible properties can not be set to private.");
            } 
        }
        
        Initialize(property, property.PropertyType);
    }

    public PropertyMetaData(FieldDefinition property) : this((MemberReference) property)
    {
        MemberRef = property;
        Initialize(property, property.FieldType);
    }
    
    private void Initialize(IMemberDefinition property, TypeReference propertyType)
    {
        Name = property.Name;
        PropertyDataType = propertyType.GetDataType(property.FullName, property.CustomAttributes);
        PropertyFlags flags = (PropertyFlags) GetFlags(property, "PropertyFlagsMapAttribute");
        
        CustomAttribute? upropertyAttribute = property.GetUProperty();
        if (upropertyAttribute == null)
        {
            return;
        }

        CustomAttributeArgument? blueprintSetterArgument = upropertyAttribute.FindAttributeField("BlueprintSetter");
        if (blueprintSetterArgument.HasValue)
        {
            BlueprintSetter = (string) blueprintSetterArgument.Value.Value;
        }
        
        CustomAttributeArgument? blueprintGetterArgument = upropertyAttribute.FindAttributeField("BlueprintGetter");
        if (blueprintGetterArgument.HasValue)
        {
            BlueprintGetter = (string) blueprintGetterArgument.Value.Value;
        }
        
        CustomAttributeArgument? lifetimeConditionField = upropertyAttribute.FindAttributeField("LifetimeCondition");
        if (lifetimeConditionField.HasValue)
        {
            LifetimeCondition = (LifetimeCondition) lifetimeConditionField.Value.Value;
        }
        
        CustomAttributeArgument? notifyMethodArgument = upropertyAttribute.FindAttributeField("ReplicatedUsing");
        if (notifyMethodArgument.HasValue)
        {
            string notifyMethodName = (string) notifyMethodArgument.Value.Value;
            MethodReference? notifyMethod = property.DeclaringType.FindMethod(notifyMethodName);
            
            if (notifyMethod == null)
            {
                throw new InvalidPropertyException(property, $"RepNotify method '{notifyMethodName}' not found on {property.DeclaringType.Name}");
            }
            
            if (!notifyMethod.Resolve().IsUFunction())
            {
                throw new InvalidPropertyException(property, $"RepNotify method '{notifyMethodName}' needs to be declared as a UFunction.");
            }

            if (!notifyMethod.ReturnsVoid())
            {
                throw new InvalidPropertyException(property, $"RepNotify method '{notifyMethodName}' must return void");
            }

            if (notifyMethod.Parameters.Count > 0)
            {
                if (notifyMethod.Parameters[0].ParameterType != propertyType)
                {
                    throw new InvalidPropertyException(property, $"RepNotify can only have matching parameters to the property it is notifying. '{notifyMethodName}' takes a '{notifyMethod.Parameters[0].ParameterType.FullName}' but the property is a '{propertyType.FullName}'");
                }
                
                if (notifyMethod.Parameters.Count > 1)
                {
                    throw new InvalidPropertyException(property, $"RepNotify method '{notifyMethodName}' must take a single argument");
                }
            }

            // Just a quality of life, if the property is set to ReplicatedUsing, it should be replicating
            flags |= PropertyFlags.Net | PropertyFlags.RepNotify;
            RepNotifyFunctionName = notifyMethodName;
        }
        
        if (flags.HasFlag(PropertyFlags.Net) && !PropertyDataType.IsNetworkSupported)
        {
            throw new InvalidPropertyException(property, $"{Name} is marked as replicated but the {PropertyDataType.CSharpType} is not supported for replication");
        }
        
        bool isDefaultComponent = NativeDataDefaultComponent.IsDefaultComponent(property.CustomAttributes);
        bool isPersistentInstance = (flags & PropertyFlags.PersistentInstance) != 0;

        const PropertyFlags instancedFlags = PropertyFlags.InstancedReference | PropertyFlags.ExportObject;
        
        if ((flags & PropertyFlags.InstancedReference) != 0 || isPersistentInstance)
        {
            flags |= instancedFlags;
        }
        
        if (isDefaultComponent)
        {
            flags = PropertyFlags.BlueprintVisible | PropertyFlags.NonTransactional | PropertyFlags.InstancedReference;
        }

        if (isPersistentInstance || isDefaultComponent)
        {
            TryAddMetaData("EditInline", "true");
        }
        
        PropertyFlags = flags;
    }
    
    public void InitializePropertyPointers(ILProcessor processor, Instruction loadNativeType, Instruction setPropertyPointer)
    {
        processor.Append(loadNativeType);
        processor.Emit(OpCodes.Ldstr, Name);
        processor.Emit(OpCodes.Call, WeaverImporter.GetNativePropertyFromNameMethod);
        processor.Append(setPropertyPointer);
    }
    
    public void InitializePropertyOffsets(ILProcessor processor, Instruction loadNativeType)
    {
        processor.Append(loadNativeType);
        processor.Emit(OpCodes.Call, WeaverImporter.GetPropertyOffset);
        processor.Emit(OpCodes.Stsfld, PropertyOffsetField);
    }
    
    public PropertyDefinition FindPropertyDefinition(TypeDefinition type)
    {
        PropertyDefinition[] definitions = (from propDef in type.Properties
            where propDef.Name == Name
            select propDef).ToArray();

        return definitions.Length > 0 ? definitions[0] : null;
    }
    
    public static PropertyMetaData FromTypeReference(TypeReference typeRef, string paramName, ParameterType modifier = ParameterType.None)
    {
        return new PropertyMetaData(typeRef, paramName, modifier);
    }
}