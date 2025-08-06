using System.Text.Json.Serialization;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.NativeTypes;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.MetaData;

public class PropertyMetaData : BaseMetaData
{
    public PropertyFlags PropertyFlags { get; set; } = PropertyFlags.None;
    public NativeDataType PropertyDataType { get; set; } = null!;
    public string RepNotifyFunctionName { get; set; } = string.Empty;
    public LifetimeCondition LifetimeCondition { get; set; } = LifetimeCondition.None;
    public string BlueprintSetter { get; set; } = string.Empty;
    public string BlueprintGetter { get; set; } = string.Empty;
    public bool HasCustomAccessors { get; set; } = false;
    [JsonIgnore]
    public PropertyDefinition? GeneratedAccessorProperty { get; set; } = null;

    // Non-serialized for JSON
    public FieldDefinition? PropertyOffsetField;
    public FieldDefinition? NativePropertyField;
    public readonly MemberReference? MemberRef;
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
        
        // Check if we have custom accessors
        bool hasCustomGetter = !getter.MethodIsCompilerGenerated();
        bool hasCustomSetter = setter != null && !setter.MethodIsCompilerGenerated();

        HasCustomAccessors = hasCustomGetter || hasCustomSetter;

        // Allow custom getter/setter implementations
        if (!HasCustomAccessors)
        {
            // Only throw exception if not a custom accessor
            if (!getter.MethodIsCompilerGenerated())
            {
                throw new InvalidPropertyException(property, "Getter can not have a body for Unreal properties unless it's a custom accessor");
            }

            if (setter != null && !setter.MethodIsCompilerGenerated())
            {
                throw new InvalidPropertyException(property, "Setter can not have a body for Unreal properties unless it's a custom accessor");
            }
        }
        else
        {
            // Register custom accessors as UFunctions if they have BlueprintGetter/Setter specified
            RegisterPropertyAccessorAsUFunction(property.GetMethod, true);
            RegisterPropertyAccessorAsUFunction(property.SetMethod, false);
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
        processor.Emit(OpCodes.Call, WeaverImporter.Instance.GetNativePropertyFromNameMethod);
        processor.Append(setPropertyPointer);
    }
    
    public void InitializePropertyOffsets(ILProcessor processor, Instruction loadNativeType)
    {
        processor.Append(loadNativeType);
        processor.Emit(OpCodes.Call, WeaverImporter.Instance.GetPropertyOffset);
        processor.Emit(OpCodes.Stsfld, PropertyOffsetField);
    }
    
    public static PropertyMetaData FromTypeReference(TypeReference typeRef, string paramName, ParameterType modifier = ParameterType.None, ParameterDefinition? parameterDefinition = null)
    {
        var metadata = new PropertyMetaData(typeRef, paramName, modifier);
        if (parameterDefinition is null) return metadata;
        metadata.AddMetaData(parameterDefinition);
        metadata.AddMetaTagsNamespace(parameterDefinition);
        return metadata;
    }
    
    private void RegisterPropertyAccessorAsUFunction(MethodDefinition accessorMethod, bool isGetter)
    {
        if (accessorMethod == null)
        {
            return;
        }

        // Set the appropriate blueprint accessor name based on getter or setter
        if (isGetter)
        {
            BlueprintGetter = accessorMethod.Name;
        }
        else
        {
            BlueprintSetter = accessorMethod.Name;
        }

        // Add UFunction attribute if not already present
        if (!accessorMethod.IsUFunction())
        {
            var ufunctionCtor = WeaverImporter.Instance.CurrentWeavingAssembly.MainModule.ImportReference(
                WeaverImporter.Instance.UFunctionAttributeConstructor);

            // Create constructor arguments array
            var ctorArgs = new[]
            {
                // First argument - FunctionFlags (combine BlueprintCallable with BlueprintPure for getters)
                new CustomAttributeArgument(
                    WeaverImporter.Instance.UInt64TypeRef,
                    (ulong)(isGetter 
                        ? EFunctionFlags.BlueprintCallable | EFunctionFlags.BlueprintPure 
                        : EFunctionFlags.BlueprintCallable))
            };

            var ufunctionAttribute = new CustomAttribute(ufunctionCtor)
            {
                ConstructorArguments = { ctorArgs[0] }
            };

            accessorMethod.CustomAttributes.Add(ufunctionAttribute);
            
            var blueprintInternalUseOnlyCtor = WeaverImporter.Instance.CurrentWeavingAssembly.MainModule.ImportReference(
                WeaverImporter.Instance.BlueprintInternalUseAttributeConstructor);
            accessorMethod.CustomAttributes.Add(new CustomAttribute(blueprintInternalUseOnlyCtor));
        }

        // Make the method public to be accessible from Blueprint
        accessorMethod.Attributes = (accessorMethod.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Public;
    }
}