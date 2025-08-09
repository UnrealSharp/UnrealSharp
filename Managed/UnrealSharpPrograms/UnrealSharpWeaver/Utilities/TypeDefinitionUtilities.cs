using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using UnrealSharpWeaver.NativeTypes;

namespace UnrealSharpWeaver.Utilities;

public static class TypeDefinitionUtilities
{
    public static readonly string UClassCallbacks = "UClassExporter";
    public static readonly string UClassAttribute = "UClassAttribute";
    
    public static readonly string UEnumAttribute = "UEnumAttribute";
    public static readonly string UStructAttribute = "UStructAttribute";
    public static readonly string UInterfaceAttribute = "UInterfaceAttribute";
    public static readonly string BlittableTypeAttribute = "BlittableTypeAttribute";
    
    public static CustomAttribute? GetUClass(this IMemberDefinition definition)
    {
        return definition.CustomAttributes.FindAttributeByType(WeaverImporter.UnrealSharpAttributesNamespace, UClassAttribute);
    }
    
    public static bool IsUClass(this IMemberDefinition definition)
    {
        return GetUClass(definition) != null;
    }
    
    public static bool IsUInterface(this TypeDefinition typeDefinition)
    {
        return GetUInterface(typeDefinition) != null;
    }
    
    public static bool IsUEnum(this TypeDefinition typeDefinition)
    {
        return GetUEnum(typeDefinition) != null;
    }
    
    public static CustomAttribute? GetUStruct(this IMemberDefinition type)
    {
        return type.CustomAttributes.FindAttributeByType(WeaverImporter.UnrealSharpAttributesNamespace, UStructAttribute);
    }
    
    public static bool IsUStruct(this IMemberDefinition definition)
    {
        return GetUStruct(definition) != null;
    }
    
    public static string GetEngineName(this IMemberDefinition memberDefinition)
    {
        IMemberDefinition currentMemberIteration = memberDefinition;
        while (currentMemberIteration != null)
        {
            CustomAttribute? genTypeAttribute = currentMemberIteration.CustomAttributes
                .FirstOrDefault(x => x.AttributeType.Name == WeaverImporter.GeneratedTypeAttribute);
            
            if (genTypeAttribute is not null)
            {
                return (string) genTypeAttribute.ConstructorArguments[0].Value;
            }

            if (memberDefinition.IsUClass() && memberDefinition.Name.StartsWith('U') ||
                memberDefinition.IsUStruct() && memberDefinition.Name.StartsWith('F'))
            {
                return memberDefinition.Name[1..];
            }
            
            if (currentMemberIteration is MethodDefinition { IsVirtual: true } virtualMethodDefinition)
            {
                if (currentMemberIteration == virtualMethodDefinition.GetBaseMethod())
                {
                    break;
                }
                
                currentMemberIteration = virtualMethodDefinition.GetBaseMethod();
            }
            else
            {
                break;
            }
        }
        
        // Same name in engine as in managed code
        return memberDefinition.Name;
    }
    
    public static CustomAttribute? GetUEnum(this TypeDefinition type)
    {
        return type.CustomAttributes.FindAttributeByType(WeaverImporter.UnrealSharpAttributesNamespace, UEnumAttribute);
    }
    
    public static CustomAttribute? GetBlittableType(this TypeDefinition type)
    {
        return type.CustomAttributes.FindAttributeByType(WeaverImporter.UnrealSharpCoreAttributesNamespace, BlittableTypeAttribute);
    }
    
    public static bool IsUnmanagedType(this TypeReference typeRef)
    {
        var typeDef = typeRef.Resolve();
    
        // Must be a value type
        if (!typeDef.IsValueType)
            return false;

        // Primitive types and enums are unmanaged
        if (typeDef.IsPrimitive || typeDef.IsEnum)
            return true;

        // For structs, recursively check all fields
        return typeDef.Fields
            .Where(f => !f.IsStatic)
            .Select(f => f.FieldType.Resolve())
            .All(IsUnmanagedType);
    }

    
    public static CustomAttribute? GetUInterface(this TypeDefinition type)
    {
        return type.CustomAttributes.FindAttributeByType(WeaverImporter.UnrealSharpAttributesNamespace, UInterfaceAttribute);
    }
    
    public static void AddGeneratedTypeAttribute(this TypeDefinition type)
    {
        CustomAttribute attribute = new CustomAttribute(WeaverImporter.Instance.GeneratedTypeCtor);
        string typeName = type.Name.Substring(1);
        string fullTypeName = type.Namespace + "." + typeName;
        attribute.ConstructorArguments.Add(new CustomAttributeArgument(WeaverImporter.Instance.CurrentWeavingAssembly.MainModule.TypeSystem.String, typeName));
        attribute.ConstructorArguments.Add(new CustomAttributeArgument(WeaverImporter.Instance.CurrentWeavingAssembly.MainModule.TypeSystem.String, fullTypeName));
        
        type.CustomAttributes.Add(attribute);
    }
    
    public static PropertyDefinition? FindPropertyByName(this TypeDefinition classOuter, string propertyName)
    {
        foreach (var property in classOuter.Properties)
        {
            if (property.Name == propertyName)
            {
                return property;
            }
        }

        return default;
    }
    
    public static bool IsChildOf(this TypeDefinition type, TypeDefinition parentType)
    {
        TypeDefinition? currentType = type;
        while (currentType != null)
        {
            if (currentType == parentType)
            {
                return true;
            }

            currentType = currentType.BaseType?.Resolve();
        }

        return false;
    }
    
    public static TypeReference FindNestedType(this TypeDefinition typeDef, string typeName)
    {
        foreach (var nestedType in typeDef.NestedTypes)
        {
            if (nestedType.Name != typeName)
            {
                continue;
            }

            return WeaverImporter.Instance.CurrentWeavingAssembly.MainModule.ImportReference(nestedType);
        }
        
        throw new Exception($"{typeName} not found in {typeDef}.");
    }
    
    public static MethodDefinition AddMethod(this TypeDefinition type, string name, TypeReference? returnType, MethodAttributes attributes = MethodAttributes.Private, params TypeReference[] parameterTypes)
    {
        returnType ??= WeaverImporter.Instance.CurrentWeavingAssembly.MainModule.TypeSystem.Void;
        
        var method = new MethodDefinition(name, attributes, returnType);
        
        foreach (var parameterType in parameterTypes)
        {
            method.Parameters.Add(new ParameterDefinition(parameterType));
        }
        type.Methods.Add(method);
        return method;
    }

    private static readonly MethodAttributes MethodAttributes = MethodAttributes.Public | MethodAttributes.Static;
    
    public static MethodDefinition AddToNativeMethod(this TypeDefinition type, TypeDefinition valueType, TypeReference[]? parameters = null)
    {
        if (parameters == null)
        {
            parameters = [WeaverImporter.Instance.IntPtrType, WeaverImporter.Instance.Int32TypeRef, valueType];
        }
        
        MethodDefinition toNativeMethod = type.AddMethod("ToNative", WeaverImporter.Instance.VoidTypeRef, MethodAttributes, parameters);
        return toNativeMethod;
    }
    
    public static MethodDefinition AddFromNativeMethod(this TypeDefinition type, TypeDefinition returnType, TypeReference[]? parameters = null)
    {
        if (parameters == null)
        {
            parameters = [WeaverImporter.Instance.IntPtrType, WeaverImporter.Instance.Int32TypeRef];
        }
        
        MethodDefinition fromNative = type.AddMethod("FromNative", returnType, MethodAttributes, parameters);
        return fromNative;
    }
    
    public static FieldDefinition AddField(this TypeDefinition type, string name, TypeReference typeReference, FieldAttributes attributes = 0)
    {
        if (attributes == 0)
        {
            attributes = FieldAttributes.Static | FieldAttributes.Private;
        }
        
        var field = new FieldDefinition(name, attributes, typeReference);
        type.Fields.Add(field);
        return field;
    }
    
    public static FieldReference FindField(this TypeDefinition typeDef, string fieldName)
    {
        foreach (var field in typeDef.Fields)
        {
            if (field.Name != fieldName)
            {
                continue;
            }

            return WeaverImporter.Instance.CurrentWeavingAssembly.MainModule.ImportReference(field);
        }
        
        throw new Exception($"{fieldName} not found in {typeDef}.");
    }
    
    public static bool IsUObject(this TypeDefinition typeDefinition)
    {
        if (!typeDefinition.IsUClass())
        {
            return false;
        }
        
        while (typeDefinition != null)
        {
            if (typeDefinition.BaseType == null)
            {
                return false;
            }
            
            if (typeDefinition == WeaverImporter.Instance.UObjectDefinition)
            {
                return true;
            }

            typeDefinition = typeDefinition.BaseType.Resolve();
        }
        
        return false;
    }
    
    public static TypeReference ImportType(this TypeReference type)
    {
        return WeaverImporter.Instance.CurrentWeavingAssembly.MainModule.ImportReference(type);
    }
    
    public static bool HasMethod(this TypeDefinition typeDef, string methodName, bool throwIfNotFound = true, params TypeReference[] parameterTypes)
    {
        return FindMethod(typeDef, methodName, throwIfNotFound, parameterTypes) != null;
    }

    public static MethodReference? FindMethod(this TypeReference typeReference, string methodName,
        bool throwIfNotFound = true, params TypeReference[] parameterTypes)
    {
        return FindMethod(typeReference.Resolve(), methodName, throwIfNotFound, parameterTypes);
    }

    public static MethodReference? FindMethod(this TypeDefinition typeDef, string methodName, bool throwIfNotFound = true, params TypeReference[] parameterTypes)
    {
        TypeDefinition? currentClass = typeDef;
        while (currentClass != null)
        {
            MethodReference? method = FindOwnMethod(currentClass, methodName, throwIfNotFound: false, parameterTypes);
            if (method != null)
            {
                return method;
            }

            currentClass = currentClass.BaseType?.Resolve();
        }

        if (throwIfNotFound)
        {
            throw new Exception("Couldn't find method " + methodName + " in " + typeDef + ".");
        }

        return default;
    }
    
    public static MethodReference? FindOwnMethod(TypeDefinition typeDef, string methodName, bool throwIfNotFound = true, params TypeReference[] parameterTypes)
    {
        foreach (var classMethod in typeDef.Methods)
        {
            if (classMethod.Name != methodName)
            {
                continue;
            }

            if (parameterTypes.Length > 0 && classMethod.Parameters.Count != parameterTypes.Length)
            {
                continue;
            }

            bool found = true;
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                if (classMethod.Parameters[i].ParameterType.FullName != parameterTypes[i].FullName)
                {
                    found = false;
                    break;
                }
            }

            if (found)
            {
                return classMethod.ImportMethod();
            }
        }

        if (throwIfNotFound)
        {
            throw new Exception("Couldn't find method " + methodName + " in " + typeDef + ".");
        }

        return default;
    }
    
    public static NativeDataType GetDataType(this TypeReference typeRef, string propertyName, Collection<CustomAttribute>? customAttributes)
    {
        int arrayDim = 1;
        TypeDefinition typeDef = typeRef.Resolve();
        SequencePoint? sequencePoint = ErrorEmitter.GetSequencePointFromMemberDefinition(typeDef);

        if (customAttributes != null)
        {
            CustomAttribute? propertyAttribute = typeDef.GetUProperty();
            
            if (propertyAttribute != null)
            {
                CustomAttributeArgument? arrayDimArg = propertyAttribute.FindAttributeField("ArrayDim");

                if (typeRef is GenericInstanceType genericType && genericType.GetElementType().FullName == "UnrealSharp.FixedSizeArrayReadWrite`1")
                {
                    if (arrayDimArg.HasValue)
                    {
                        arrayDim = (int) arrayDimArg.Value.Value;

                        // Unreal doesn't have a separate type for fixed arrays, so we just want to generate the inner UProperty type with an arrayDim.
                        typeRef = genericType.GenericArguments[0];
                        typeDef = typeRef.Resolve();
                    }
                    else
                    {
                        throw new InvalidPropertyException(propertyName, sequencePoint, "Fixed array properties must specify an ArrayDim in their [UProperty] attribute");
                    }
                }
                else if (arrayDimArg.HasValue)
                {
                    throw new InvalidPropertyException(propertyName, sequencePoint, "ArrayDim is only valid for FixedSizeArray properties.");
                }
            }
        }

        switch (typeDef.FullName)
        {
            case "System.Double":
                return new NativeDataBuiltinType(typeRef, arrayDim, PropertyType.Double);
            case "System.Single":
                return new NativeDataBuiltinType(typeRef, arrayDim, PropertyType.Float);

            case "System.SByte":
                return new NativeDataBuiltinType(typeRef, arrayDim, PropertyType.Int8);
            case "System.Int16":
                return new NativeDataBuiltinType(typeRef, arrayDim, PropertyType.Int16);
            case "System.Int32":
                return new NativeDataBuiltinType(typeRef, arrayDim, PropertyType.Int);
            case "System.Int64":
                return new NativeDataBuiltinType(typeRef, arrayDim, PropertyType.Int64);

            case "System.Byte":
                return new NativeDataBuiltinType(typeRef, arrayDim, PropertyType.Byte);
            case "System.UInt16":
                return new NativeDataBuiltinType(typeRef, arrayDim, PropertyType.UInt16);
            case "System.UInt32":
                return new NativeDataBuiltinType(typeRef, arrayDim, PropertyType.UInt32);
            case "System.UInt64":
                return new NativeDataBuiltinType(typeRef, arrayDim, PropertyType.UInt64);

            case "System.Boolean":
                return new NativeDataBooleanType(typeRef, arrayDim);

            case "System.String":
                return new NativeDataStringType(typeRef, arrayDim);

            default:

                if (typeRef.IsGenericInstance || typeRef.IsByReference)
                {
                    GenericInstanceType? instanceType = null;
                    if (typeRef is GenericInstanceType genericInstanceType)
                    {
                        instanceType = genericInstanceType;
                    }
                    if (typeRef is ByReferenceType byReferenceType)
                    {
                        instanceType = byReferenceType.ElementType as GenericInstanceType;
                        typeRef = byReferenceType.ElementType;
                    }

                    if (instanceType != null)
                    {
                        TypeReference[] genericArguments = instanceType.GenericArguments.ToArray();
                        string? genericTypeName = instanceType.ElementType.Name;
                        
                        if (genericTypeName.Contains("TArray`1") || genericTypeName.Contains("List`1"))
                        {
                            return new NativeDataArrayType(typeRef, arrayDim, genericArguments[0]);
                        }

                        if (genericTypeName.Contains("TNativeArray`1") || genericTypeName.Contains("ReadOnlySpan`1"))
                        {
                            return new NativeDataNativeArrayType(typeRef, arrayDim, genericArguments[0]);
                        }

                        if (genericTypeName.Contains("TMap`2") || genericTypeName.Contains("Dictionary`2"))
                        {
                            return new NativeDataMapType(typeRef, arrayDim, genericArguments[0], genericArguments[1]);
                        }
                        
                        if (genericTypeName.Contains("TSet`1") || genericTypeName.Contains("HashSet`1"))
                        {
                            return new NativeDataSetType(typeRef, arrayDim, genericArguments[0]);
                        }

                        if (genericTypeName.Contains("TSubclassOf`1"))
                        {
                            return new NativeDataClassType(typeRef, genericArguments[0], arrayDim);
                        }

                        if (genericTypeName.Contains("TWeakObjectPtr`1"))
                        {
                            return new NativeDataWeakObjectType(typeRef, genericArguments[0], arrayDim);
                        }

                        if (genericTypeName.Contains("TSoftObjectPtr`1"))
                        {
                            return new NativeDataSoftObjectType(typeRef, genericArguments[0], arrayDim);
                        }

                        if (genericTypeName.Contains("TSoftClassPtr`1"))
                        {
                            return new NativeDataSoftClassType(typeRef, genericArguments[0], arrayDim);
                        }

                        if (genericTypeName.Contains("Option`1"))
                        {
                            return new NativeDataOptionalType(typeRef, genericArguments[0], arrayDim);
                        }
                    }
                }

                if (typeDef.IsEnum && typeDef.IsUEnum())
                {
                    CustomAttribute? enumAttribute = typeDef.GetUEnum();
                
                    if (enumAttribute == null)
                    {
                        throw new InvalidPropertyException(propertyName, sequencePoint, "Enum properties must use an UEnum enum: " + typeRef.FullName);
                    }
                
                    // TODO: This is just true for properties, not for function parameters they can be int. Need a good way to differentiate.
                    // if (typeDef.GetEnumUnderlyingType().Resolve() != ByteTypeRef.Resolve())
                    // {
                    //     throw new InvalidPropertyException(propertyName, sequencePoint, "Enum's exposed to Blueprints must have an underlying type of System.Byte: " + typeRef.FullName);
                    // }

                    return new NativeDataEnumType(typeDef, arrayDim);
                }

                if (typeDef.IsInterface && typeDef.IsUInterface())
                {
                    return new NativeDataInterfaceType(typeRef, typeDef.Name + "Marshaller");
                }
                
                if (typeDef.FullName == "UnrealSharp.FText")
                {
                    return new NativeDataTextType(typeDef);
                }
                
                if (typeDef.FullName == "UnrealSharp.FName")
                {
                    return new NativeDataNameType(typeDef, arrayDim);
                }
            
                if (typeDef.Name == "TMulticastDelegate`1")
                {
                    return new NativeDataMulticastDelegate(typeRef);
                }
            
                if (typeDef.Name == "TDelegate`1")
                {
                    return new NativeDataDelegateType(typeRef);
                }
            
                if (customAttributes != null && NativeDataDefaultComponent.IsDefaultComponent(customAttributes))
                {
                    return new NativeDataDefaultComponent(customAttributes, typeDef, arrayDim);
                }
            
                TypeDefinition? superType = typeDef;
                while (superType != null && superType.FullName != "UnrealSharp.Core.UnrealSharpObject")
                {
                    TypeReference superTypeRef = superType.BaseType;
                    superType = superTypeRef != null ? superTypeRef.Resolve() : null;
                }

                if (superType != null)
                {
                    return new NativeDataObjectType(typeRef, typeDef, arrayDim);
                }

                // See if this is a struct
                CustomAttribute? structAttribute = typeDef.GetUStruct();
                
                if (structAttribute == null)
                {
                    return typeDef.IsUnmanagedType() ? new NativeDataUnmanagedType(typeDef, arrayDim) : new NativeDataManagedObjectType(typeRef, arrayDim);
                }
                
                return typeDef.GetBlittableType() != null ? new NativeDataBlittableStructType(typeDef, arrayDim) : new NativeDataStructType(typeDef, typeDef.GetMarshallerClassName(), arrayDim);
        }
    }
    
    public static string GetWrapperClassName(this TypeReference typeRef)
    {
        return typeRef.Name + "Wrapper";
    }
    
    public static string GetMarshallerClassName(this TypeReference typeRef)
    {
        return typeRef.Name + "Marshaller";
    }
    
    public static PropertyType GetPrimitiveTypeCode(this TypeReference type)
    {
        // Is there a better way to do this? The private member e_type on TypeReference has what we want
        return type.FullName switch
        {
            "System.Byte" => PropertyType.Byte,
            "System.SByte" => PropertyType.Int8,
            "System.Int16" => PropertyType.Int16,
            "System.UInt16" => PropertyType.UInt16,
            "System.Int32" => PropertyType.Int,
            "System.UInt32" => PropertyType.UInt32,
            "System.Int64" => PropertyType.Int64,
            "System.UInt64" => PropertyType.UInt64,
            "System.Float" => PropertyType.Float,
            "System.Double" => PropertyType.Double,
            _ => throw new NotImplementedException()
        };
    }
}