using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.NativeTypes;
using UnrealSharpWeaver.Rewriters;

namespace UnrealSharpWeaver.MetaData;

public class PropertyMetaData : BaseMetaData
{
    public PropertyFlags PropertyFlags { get; set; }
    public NativeDataType PropertyDataType { get; set; }
    public AccessProtection AccessProtection { get; set; }
    public string RepNotifyFunctionName { get; set; }
    public LifetimeCondition LifetimeCondition { get; set; }
    public string BlueprintSetter { get; set; }
    public string BlueprintGetter { get; set; }
    
    // Non-serialized for JSON
    public FieldDefinition PropertyOffsetField;
    public FieldDefinition? NativePropertyField;
    public readonly MemberReference MemberRef;
    // End non-serialized
    
    static bool MethodIsCompilerGenerated(MethodDefinition method)
    {
        return WeaverHelper.FindAttributeByType(method.CustomAttributes, "System.Runtime.CompilerServices", "CompilerGeneratedAttribute") != null;
    }

    private PropertyMetaData(TypeReference typeRef, string paramName, ParameterType modifier)
    {
        MemberRef = typeRef;
        Name = paramName;
        PropertyDataType = WeaverHelper.GetDataType(typeRef, paramName, null);
        
        PropertyFlags flags = PropertyFlags.None;
        
        if (modifier != ParameterType.None)
        {
            flags |= PropertyFlags.Parm;
        }
        
        if (modifier == ParameterType.Out)
        {
            flags |= PropertyFlags.OutParm;
        }
        else if (modifier == ParameterType.Ref)
        {
            flags |= PropertyFlags.OutParm | PropertyFlags.ReferenceParm;
        }
        else if (modifier == ParameterType.ReturnValue)
        {
            flags |= PropertyFlags.ReturnParm;
        }

        PropertyFlags = flags;
    }
    
    public static PropertyMetaData FromTypeReference(TypeReference typeRef, string paramName, ParameterType modifier = ParameterType.None)
    {
        return new PropertyMetaData(typeRef, paramName, modifier);
    }

    public PropertyMetaData(PropertyDefinition property)
    {
        MemberRef = property;
        
        MethodDefinition getter = property.GetMethod;
        MethodDefinition setter = property.SetMethod;

        if (getter == null)
        {
            throw new InvalidPropertyException(property, "Unreal properties must have a default get method");
        }

        if (!MethodIsCompilerGenerated(getter))
        {
            throw new InvalidPropertyException(property, "Getter can not have a body for Unreal properties");
        }

        if (setter != null && ! MethodIsCompilerGenerated(setter))
        {
            throw new InvalidPropertyException(property, "Setter can not have a body for Unreal properties");
        }

        if (getter.IsPrivate)
        {
            AccessProtection = AccessProtection.Private;
        }
        else if (getter.IsPublic)
        {
            AccessProtection = AccessProtection.Public;
        }
        else
        {
            // if not private or public, assume protected?
            AccessProtection = AccessProtection.Protected;
        }
        Initialize(property, property.PropertyType);
    }

    public PropertyMetaData(FieldDefinition property)
    {
        MemberRef = property;
        
        if (property.IsPrivate)
        {
            AccessProtection = AccessProtection.Private;
        }
        else if (property.IsPublic)
        {
            AccessProtection = AccessProtection.Public;
        }
        else
        {
            AccessProtection = AccessProtection.Protected;
        }
        
        Initialize(property, property.FieldType);
    }
    
    private void Initialize(IMemberDefinition property, TypeReference propertyType)
    {
        Name = property.Name;
        PropertyDataType = WeaverHelper.GetDataType(propertyType, property.FullName, property.CustomAttributes);
        PropertyFlags flags = (PropertyFlags) GetFlags(property, "PropertyFlagsMapAttribute");
        
        AddMetadataAttributes(property.CustomAttributes);

        // do some extra verification, matches verification in UE4 header parser
        if (AccessProtection == AccessProtection.Private && (flags & PropertyFlags.BlueprintVisible) != 0)
        {
            if(!GetBoolMetadata(MetaData, "AllowPrivateAccess"))
            {
                throw new InvalidPropertyException(property, "Blueprint visible properties can not be private");
            }                
        }
        
        CustomAttribute? upropertyAttribute = FindAttribute(property.CustomAttributes, "UPropertyAttribute");

        AddBaseAttributes(upropertyAttribute);

        CustomAttributeArgument? blueprintSetterArgument = WeaverHelper.FindAttributeField(upropertyAttribute, "BlueprintSetter");
        
        if (blueprintSetterArgument.HasValue)
        {
            BlueprintSetter = (string) blueprintSetterArgument.Value.Value;
        }
        
        CustomAttributeArgument? blueprintGetterArgument = WeaverHelper.FindAttributeField(upropertyAttribute, "BlueprintGetter");

        if (blueprintGetterArgument.HasValue)
        {
            BlueprintGetter = (string) blueprintGetterArgument.Value.Value;
        }
        
        CustomAttributeArgument? lifetimeConditionField = WeaverHelper.FindAttributeField(upropertyAttribute, "LifetimeCondition");

        if (lifetimeConditionField.HasValue)
        {
            LifetimeCondition = (LifetimeCondition) lifetimeConditionField.Value.Value;
        }
        
        CustomAttributeArgument? notifyMethodArgument = WeaverHelper.FindAttributeField(upropertyAttribute, "ReplicatedUsing");
        
        if (notifyMethodArgument.HasValue)
        {
            string notifyMethodName = (string) notifyMethodArgument.Value.Value;
            MethodReference? notifyMethod = WeaverHelper.FindMethod(property.DeclaringType, notifyMethodName);
            
            if (notifyMethod == null)
            {
                throw new InvalidPropertyException(property, $"RepNotify method '{notifyMethodName}' not found on {property.DeclaringType.Name}");
            }

            if (notifyMethod.ReturnType != WeaverHelper.VoidTypeRef)
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

            if (!FunctionMetaData.IsUFunction(notifyMethod.Resolve()))
            {
                throw new InvalidPropertyException(property, $"RepNotify method '{notifyMethodName}' needs to be declared as a UFunction.");
            }

            // Just a quality of life, if the property is set to ReplicatedUsing, it should be replicating
            flags |= PropertyFlags.Net;
            
            RepNotifyFunctionName = notifyMethodName;
        }
        
        if (NativeDataDefaultComponent.IsDefaultComponent(property.CustomAttributes))
        {
            flags = PropertyFlags.InstancedReference | PropertyFlags.ExportObject | PropertyFlags.Edit | PropertyFlags.EditConst | PropertyFlags.BlueprintReadOnly | PropertyFlags.BlueprintVisible;
        }
        
        PropertyFlags = flags;
    }
    
    public PropertyDefinition FindPropertyDefinition(TypeDefinition type)
    {
        PropertyDefinition[] definitions = (from propDef in type.Properties
            where propDef.Name == Name
            select propDef).ToArray();

        return definitions.Length > 0 ? definitions[0] : null;
    }

    public static CustomAttribute? GetUPropertyAttribute(PropertyDefinition property)
    {
        return FindAttribute(property.CustomAttributes, "UPropertyAttribute");
    }

    public static bool IsOutParameter(PropertyFlags propertyFlags)
    {
        return (propertyFlags & PropertyFlags.OutParm) == PropertyFlags.OutParm;
    }
    
    public void InitializePropertyPointers(ILProcessor processor, Instruction loadNativeType, Instruction setPropertyPointer)
    {
        processor.Append(loadNativeType);
        processor.Emit(OpCodes.Ldstr, Name);
        processor.Emit(OpCodes.Call, WeaverHelper.GetNativePropertyFromNameMethod);
        processor.Append(setPropertyPointer);
    }
    
    public void InitializePropertyOffsets(ILProcessor processor, Instruction loadNativeType)
    {
        processor.Append(loadNativeType);
        processor.Emit(OpCodes.Call, WeaverHelper.GetPropertyOffset);
        processor.Emit(OpCodes.Stsfld, PropertyOffsetField);
    }
}