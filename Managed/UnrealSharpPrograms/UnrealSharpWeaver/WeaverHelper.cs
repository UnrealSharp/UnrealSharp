using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using UnrealSharpWeaver.NativeTypes;

namespace UnrealSharpWeaver;

public static class WeaverHelper
{
    public static AssemblyDefinition UserAssembly;
    public static AssemblyDefinition BindingsAssembly;
    public static MethodReference NativeObjectGetter;
    public static TypeDefinition IntPtrType;
    public static MethodReference IntPtrAdd;
    public static FieldReference IntPtrZero;
    public static MethodReference IntPtrEqualsOperator;
    public static TypeReference UnrealSharpObjectType;
    public static MethodReference CheckObjectForValidity;
    public static MethodReference GetNativeFunctionFromInstanceAndNameMethod;
    public static TypeReference Uint16TypeRef;
    public static TypeReference Int32TypeRef;
    public static TypeReference VoidTypeRef;
    public static TypeReference ByteTypeRef;
    public static MethodReference GetNativeClassFromNameMethod;
    public static MethodReference GetNativeStructFromNameMethod;
    public static MethodReference GetPropertyOffsetFromNameMethod;
    public static MethodReference GetPropertyOffset;
    public static MethodReference GetArrayElementSizeMethod;
    public static MethodReference GetNativePropertyFromNameMethod;
    public static MethodReference GetNativeFunctionFromClassAndNameMethod;
    public static MethodReference GetNativeFunctionParamsSizeMethod;
    public static MethodReference GetNativeStructSizeMethod;
    public static MethodReference InvokeNativeFunctionMethod;
    public static MethodReference GetSignatureFunction;
    
    public static void Initialize(AssemblyDefinition bindingsAssembly)
    {
        BindingsAssembly = bindingsAssembly;
    }

    public static void ImportCommonTypes(AssemblyDefinition userAssembly)
    {
        UserAssembly = userAssembly;
        
        TypeSystem typeSystem = UserAssembly.MainModule.TypeSystem;
        Uint16TypeRef = typeSystem.UInt16;
        Int32TypeRef = typeSystem.Int32;
        VoidTypeRef = typeSystem.Void;
        ByteTypeRef = typeSystem.Byte;
        
        IntPtrType = typeSystem.IntPtr.Resolve();
        IntPtrAdd = FindMethod(IntPtrType, "Add")!;
        IntPtrZero = FindFieldInType(IntPtrType, "Zero");
        IntPtrEqualsOperator = FindMethod(IntPtrType, "op_Equality")!;

        UnrealSharpObjectType = FindTypeInAssembly(BindingsAssembly, Program.UnrealSharpNamespace, Program.UnrealSharpObjectName);
        
        TypeDefinition unrealSharpObjectType = UnrealSharpObjectType.Resolve();
        NativeObjectGetter = FindMethod(unrealSharpObjectType, "get_NativeObject")!;
        CheckObjectForValidity = FindMethod(unrealSharpObjectType, "CheckObjectForValidity")!;

        GetNativeFunctionFromInstanceAndNameMethod = FindExporterMethod(Program.UClassCallbacks, "CallGetNativeFunctionFromInstanceAndName");
        GetNativeStructFromNameMethod = FindExporterMethod(Program.CoreUObjectCallbacks, "CallGetNativeStructFromName");
        GetNativeClassFromNameMethod = FindExporterMethod(Program.CoreUObjectCallbacks, "CallGetNativeClassFromName");
        GetPropertyOffsetFromNameMethod = FindExporterMethod(Program.FPropertyCallbacks, "CallGetPropertyOffsetFromName");
        GetPropertyOffset = FindExporterMethod(Program.FPropertyCallbacks, "CallGetPropertyOffset");
        GetArrayElementSizeMethod = FindExporterMethod(Program.FArrayPropertyCallbacks, "CallGetArrayElementSize");
        GetNativePropertyFromNameMethod = FindExporterMethod(Program.FPropertyCallbacks, "CallGetNativePropertyFromName");
        GetNativeFunctionFromClassAndNameMethod = FindExporterMethod(Program.UClassCallbacks, "CallGetNativeFunctionFromClassAndName");
        GetNativeFunctionParamsSizeMethod = FindExporterMethod(Program.UFunctionCallbacks, "CallGetNativeFunctionParamsSize");
        GetNativeStructSizeMethod = FindExporterMethod(Program.UScriptStructCallbacks, "CallGetNativeStructSize");
        InvokeNativeFunctionMethod = FindExporterMethod(Program.UObjectCallbacks, "CallInvokeNativeFunction");
        GetSignatureFunction = FindExporterMethod(Program.MulticastDelegatePropertyCallbacks, "CallGetSignatureFunction");
        
    }
    
    public static TypeReference FindGenericTypeInAssembly(AssemblyDefinition assembly, string typeNamespace, string typeName, TypeReference[] typeParameters)
    {
        TypeReference? typeRef = FindTypeInAssembly(assembly, typeNamespace, typeName);
        return UserAssembly.MainModule.ImportReference(typeRef.Resolve().MakeGenericInstanceType(typeParameters));
    }

    public static TypeReference? FindTypeInAssembly(AssemblyDefinition assembly, string typeNamespace, string typeName, bool throwOnException = true)
    {
        foreach (var module in assembly.Modules)
        {
            foreach (var type in module.GetAllTypes())
            {
                if (type.Namespace != typeNamespace || type.Name != typeName)
                {
                    continue;
                }
                
                return UserAssembly.MainModule.ImportReference(type);
            }
        }

        if (throwOnException)
        {
            throw new TypeAccessException($"Type \"{typeNamespace}.{typeName}\" not found in userAssembly {assembly.Name}");
        }

        return null;
    }

    public static FieldReference FindFieldInType(TypeDefinition typeDef, string fieldName)
    {
        foreach (var field in typeDef.Fields)
        {
            if (field.Name != fieldName)
            {
                continue;
            }

            return UserAssembly.MainModule.ImportReference(field);
        }
        
        throw new Exception($"{fieldName} not found in {typeDef}.");
    }

    public static MethodReference FindBindingsStaticMethod(string findNamespace, string findClass, string findMethod)
    {
        foreach (var module in BindingsAssembly.Modules)
        {
            foreach (var type in module.GetAllTypes())
            {
                if (type.Namespace != findNamespace || type.Name != findClass)
                {
                    continue;
                }

                foreach (var method in type.Methods)
                {
                    if (method.IsStatic && method.Name == findMethod)
                    {
                        return UserAssembly.MainModule.ImportReference(method);
                    }
                }
            }
        }
        
        throw new Exception("Could not find method " + findMethod + " in class " + findClass + " in namespace " + findNamespace);
    }

    public static MethodReference FindExporterMethod(string exporterName, string functionName)
    {
        return FindBindingsStaticMethod(Program.InteropNameSpace, exporterName, functionName);
    }

    public static FieldDefinition AddOffsetFieldToType(TypeDefinition type, string name, TypeReference int32TypeRef)
    {
        return AddFieldToType(type, name, int32TypeRef);
    }

    public static FieldDefinition AddFieldToType(TypeDefinition type, string name, TypeReference typeReference, FieldAttributes attributes = 0)
    {
        if (attributes == 0)
        {
            attributes = FieldAttributes.Static | FieldAttributes.Private;
        }
        
        var field = new FieldDefinition(name, attributes, typeReference);
        type.Fields.Add(field);
        return field;
    }
    
    public static ParameterDefinition AddParameterToMethod(MethodReference method, string name, TypeReference typeReference)
    {
        var parameter = new ParameterDefinition(name, ParameterAttributes.None, typeReference);
        method.Parameters.Add(parameter);
        return parameter;
    }
    
    public static ParameterDefinition AddParameterToMethod(MethodReference method, TypeReference typeReference)
    {
        var parameter = new ParameterDefinition(typeReference);
        method.Parameters.Add(parameter);
        return parameter;
    }
    
    public static MethodDefinition CopyMethod(MethodDefinition method, bool overrideMethod)
    {
        return CopyMethod(method, overrideMethod, method.ReturnType);
    }

    public static MethodDefinition CopyMethod(MethodDefinition method, bool overrideMethod, TypeReference returnType)
    {
        MethodDefinition newMethod = new MethodDefinition(method.Name, method.Attributes, returnType);

        if (overrideMethod)
        {
            newMethod.Attributes &= ~MethodAttributes.VtableLayoutMask;
            newMethod.Attributes &= ~MethodAttributes.NewSlot;
            newMethod.Attributes |= MethodAttributes.ReuseSlot;
        }

        newMethod.HasThis = true;
        newMethod.ExplicitThis = method.ExplicitThis;
        newMethod.CallingConvention = method.CallingConvention;

        foreach (ParameterDefinition parameter in method.Parameters)
        {
            TypeReference importedType = ImportType(parameter.ParameterType);
            newMethod.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, importedType));
        }

        return newMethod;
    }
    
    public static MethodDefinition AddMethodToType(TypeDefinition type, string name, TypeReference? returnType, MethodAttributes attributes = MethodAttributes.Private, params TypeReference[] parameterTypes)
    {
        returnType ??= UserAssembly.MainModule.TypeSystem.Void;
        
        var method = new MethodDefinition(name, attributes, returnType);
        
        foreach (var parameterType in parameterTypes)
        {
            method.Parameters.Add(new ParameterDefinition(parameterType));
        }
        type.Methods.Add(method);
        return method;
    }
    
    public static VariableDefinition AddVariableToMethod(MethodDefinition method, TypeReference typeReference)
    {
        var variable = new VariableDefinition(typeReference);
        method.Body.Variables.Add(variable);
        return variable;
    }

    public static TypeReference ImportType(TypeReference type)
    {
        return UserAssembly.MainModule.ImportReference(type);
    }
    
    public static MethodReference ImportMethod(MethodReference method)
    {
        return UserAssembly.MainModule.ImportReference(method);
    }
    
    public static TypeReference FindNestedType(TypeDefinition typeDef, string typeName)
    {
        foreach (var nestedType in typeDef.NestedTypes)
        {
            if (nestedType.Name != typeName)
            {
                continue;
            }

            return UserAssembly.MainModule.ImportReference(nestedType);
        }
        
        throw new Exception($"{typeName} not found in {typeDef}.");
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
                return ImportMethod(classMethod);
            }
        }

        if (throwIfNotFound)
        {
            throw new Exception("Couldn't find method " + methodName + " in " + typeDef + ".");
        }

        return default;
    }

    public static MethodReference? FindMethod(TypeDefinition typeDef, string methodName, bool throwIfNotFound = true, params TypeReference[] parameterTypes)
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
    
    public static TypeDefinition CreateNewClass(AssemblyDefinition assembly, string classNamespace, string className, TypeAttributes attributes, TypeReference? parentClass = null)
    {
        if (parentClass == null)
        {
            parentClass = assembly.MainModule.TypeSystem.Object;
        }
        
        TypeDefinition newType = new TypeDefinition(classNamespace, className, attributes, parentClass);
        assembly.MainModule.Types.Add(newType);
        return newType;
    }
    
    public static void OptimizeMethod(MethodDefinition method)
    {
        if (method.Body.CodeSize == 0)
        {
            return;
        }
        
        if (method.Body.Variables.Count > 0)
        {
            method.Body.InitLocals = true;
        }
        
        method.Body.Optimize();
        method.Body.SimplifyMacros();
    }
    
    public static void RemoveReturnInstruction(MethodDefinition method)
    {
        if (method.Body.Instructions.Count > 0 && method.Body.Instructions[^1].OpCode == OpCodes.Ret)
        {
            method.Body.Instructions.RemoveAt(method.Body.Instructions.Count - 1);
        }
    }

    public static MethodDefinition AddToNativeMethod(TypeDefinition type, TypeDefinition valueType, TypeReference[]? parameters = null)
    {
        if (parameters == null)
        {
            parameters = [IntPtrType, Int32TypeRef, UnrealSharpObjectType, valueType];
        }
        
        MethodDefinition toNativeMethod = AddMethodToType(type, "ToNative", 
            VoidTypeRef,
            MethodAttributes.Public | MethodAttributes.Static, parameters);

        return toNativeMethod;
    }
    
    public static MethodDefinition AddFromNativeMethod(TypeDefinition type, TypeDefinition returnType, TypeReference[]? parameters = null)
    {
        if (parameters == null)
        {
            parameters = [IntPtrType, Int32TypeRef, UnrealSharpObjectType];
        }
        
        MethodDefinition fromNative = AddMethodToType(type, "FromNative", 
            returnType,
            MethodAttributes.Public | MethodAttributes.Static, parameters);

        return fromNative;
    }
    
    public static NativeDataType GetDataType(TypeReference typeRef, string propertyName, Collection<CustomAttribute>? customAttributes)
    {
        int arrayDim = 1;
        TypeDefinition typeDef = typeRef.Resolve();
        SequencePoint sequencePoint = ErrorEmitter.GetSequencePointFromMemberDefinition(typeDef);

        if (customAttributes != null)
        {
            CustomAttribute? propertyAttribute = FindAttributeByType(customAttributes, Program.UnrealSharpNamespace + ".Attributes", "UPropertyAttribute");
            
            if (propertyAttribute != null)
            {
                CustomAttributeArgument? arrayDimArg = FindAttributeField(propertyAttribute, "ArrayDim");

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

        switch (typeRef.FullName)
        {
            case "System.Double":
                return new NativeDataBuiltinType(typeRef, "DoubleProperty", arrayDim, PropertyType.Double);
            case "System.Single":
                return new NativeDataBuiltinType(typeRef, "FloatProperty", arrayDim, PropertyType.Float);

            case "System.SByte":
                return new NativeDataBuiltinType(typeRef, "Int8Property", arrayDim, PropertyType.Int8);
            case "System.Int16":
                return new NativeDataBuiltinType(typeRef, "Int16Property", arrayDim, PropertyType.Int16);
            case "System.Int32":
                return new NativeDataBuiltinType(typeRef, "IntProperty", arrayDim, PropertyType.Int);
            case "System.Int64":
                return new NativeDataBuiltinType(typeRef, "Int64Property", arrayDim, PropertyType.Int64);

            case "System.Byte":
                return new NativeDataBuiltinType(typeRef, "ByteProperty", arrayDim, PropertyType.Byte);
            case "System.UInt16":
                return new NativeDataBuiltinType(typeRef, "UInt16Property", arrayDim, PropertyType.UInt16);
            case "System.UInt32":
                return new NativeDataBuiltinType(typeRef,"UInt32Property", arrayDim, PropertyType.UInt32);
            case "System.UInt64":
                return new NativeDataBuiltinType(typeRef, "UInt64Property", arrayDim, PropertyType.UInt64);

            case "System.Boolean":
                return new NativeDataBooleanType(typeRef, "BoolProperty", arrayDim);

            case "System.String":
                return new NativeDataStringType(typeRef, arrayDim);

            default:
                
            if (typeRef.IsGenericInstance)
            {
                GenericInstanceType GenericType = (GenericInstanceType) typeRef;
                var GenericTypeName = GenericType.Name;
                TypeReference innerType = GenericType.GenericArguments[0];
                
                if (GenericTypeName.Contains("SubclassOf`1"))
                {
                    return new NativeDataClassType(typeRef, innerType, arrayDim);
                }
                
                if (GenericTypeName.Contains("UnrealArrayReadWrite`1"))
                {
                    return new NativeDataArrayType(typeRef, arrayDim, innerType);
                }

                if (GenericTypeName.Contains("WeakObject`1"))
                {
                    return new NativeDataWeakObjectType(typeRef, innerType, arrayDim);
                }
                
                if (GenericTypeName.Contains("SoftObject`1"))
                {
                    return new NativeDataSoftObjectType(typeRef, innerType, arrayDim);
                }
                
                if (GenericTypeName.Contains("SoftClass`1"))
                {
                    return new NativeDataSoftClassType(typeRef, innerType, arrayDim);
                }
            }

            if (typeDef.IsEnum)
            {
                CustomAttribute? enumAttribute = FindAttributeByType(typeDef.CustomAttributes, Program.UnrealSharpNamespace + ".Attributes", "UEnumAttribute");
                
                if (enumAttribute == null)
                {
                    throw new InvalidPropertyException(propertyName, sequencePoint, "Enum properties must use an unreal enum: " + typeRef.FullName);
                }
                
                if (typeDef.GetEnumUnderlyingType().FullName != "System.Byte")
                {
                    throw new InvalidPropertyException(propertyName, sequencePoint, "Enum properties must have an underlying type of System.Byte: " + typeRef.FullName);
                }

                return new NativeDataEnumType(typeDef, arrayDim);
            }

            if (!typeDef.IsClass)
            {
                throw new InvalidPropertyException(propertyName, sequencePoint, "No Unreal type for " + typeRef.FullName);
            }

            // see if its a UObject
            if (typeDef.Namespace == Program.UnrealSharpNamespace && typeDef.Name == "Text")
            {
                return new NativeDataTextType(typeRef);
            }
            
            if (typeDef.BaseType.Name.Contains("MulticastDelegate"))
            {
                return new NativeDataMulticastDelegate(typeDef);
            }
            
            if (typeDef.BaseType.Name.Contains("Delegate"))
            {
                return new NativeDataDelegateType(typeRef, typeDef.Name + "Marshaller");
            }
            
            if (NativeDataDefaultComponent.IsDefaultComponent(customAttributes))
            {
                return new NativeDataDefaultComponent(customAttributes, typeDef, "ObjectMarshaller`1", arrayDim);
            }
            
            TypeDefinition superType = typeDef;
            while (superType != null && superType.FullName != "UnrealSharp.UnrealSharpObject")
            {
                TypeReference superTypeRef = superType.BaseType;
                superType = superTypeRef != null ? superTypeRef.Resolve() : null;
            }

            if (superType != null)
            {
                return new NativeDataObjectType(typeRef, typeDef, arrayDim);
            }

            // See if this is a struct
            CustomAttribute? structAttribute = FindAttributeByType(typeDef.CustomAttributes, Program.UnrealSharpNamespace + ".Attributes", "UStructAttribute");
                
            if (structAttribute == null && typeDef.Namespace != "System.DoubleNumerics")
            {
                throw new InvalidPropertyException(propertyName, sequencePoint, "Class properties must use an unreal class: " + typeRef.FullName);
            }
                
            if (typeDef.Namespace == Program.UnrealSharpNamespace && typeDef.Name == "Name")
            {
                return new NativeDataNameType(typeDef, arrayDim);
            }

            if (typeDef.Namespace == "System.DoubleNumerics" || (typeDef.Namespace == Program.UnrealSharpNamespace && typeDef.Name == "Rotator"))
            {
                return new NativeDataCoreStructType(typeDef, arrayDim);
            }
                
            bool isBlittable = false;
            var blittableAttrib = FindAttributeField(structAttribute, "IsBlittable");
                        
            if (blittableAttrib.HasValue)
            {
                isBlittable = (bool) blittableAttrib.Value.Value;
            }
                        
            return isBlittable ? new NativeDataBlittableStructType(typeDef, arrayDim) : new NativeDataStructType(typeDef, GetMarshallerClassName(typeDef), arrayDim);
        }
    }
    
    public static string GetMarshallerClassName(TypeReference typeRef)
    {
        return typeRef.Name + "Marshaller";
    }
    
    public static CustomAttribute?[] FindMetaDataAttributes(IEnumerable<CustomAttribute> customAttributes)
    {
        return FindAttributesByType(customAttributes, Program.UnrealSharpNamespace, "UMetaDataAttribute");
    }

    static bool HasAttribute(TypeDefinition type, string attributeName)
    {
        return type.CustomAttributes.Any(attr => attr.AttributeType.Name == attributeName);
    }

    public static bool IsUnrealSharpEnum(TypeDefinition type)
    {
        return IsUnrealType(type, "UEnumAttribute");
    }

    public static bool IsUnrealSharpClass(TypeDefinition type)
    {
        return IsUnrealType(type, "UClassAttribute");
    }

    public static bool IsUnrealSharpStruct(TypeDefinition type)
    {
        return IsUnrealType(type, "UStructAttribute");
    }
    
    public static bool IsUnrealSharpInterface(TypeDefinition type)
    {
        return IsUnrealType(type, "UInterfaceAttribute");
    }

    static bool IsUnrealType(TypeDefinition type, string attribute)
    {
        bool bHasAttribute = HasAttribute(type, attribute);
        bool bIsUserCreated = !type.Namespace.StartsWith("UnrealSharp");
        return bHasAttribute && bIsUserCreated;
    }

    public static CustomAttributeArgument? FindAttributeField(CustomAttribute? attribute, string fieldName)
    {
        foreach (var field in attribute.Fields) 
        {
            if (field.Name == fieldName) 
            {
                return field.Argument;
            }
        }
        return null;
    }
    
    public static CustomAttribute? FindAttributeByType(IEnumerable<CustomAttribute> customAttributes, string typeNamespace, string typeName)
    {
        CustomAttribute?[] attribs = FindAttributesByType (customAttributes, typeNamespace, typeName);

        if (attribs.Length == 0) 
        {
            return null;
        }
        return attribs [0];
    }

    public static CustomAttribute?[] FindAttributesByType(IEnumerable<CustomAttribute> customAttributes, string typeNamespace, string typeName)
    {
        return (from attrib in customAttributes
            where attrib.AttributeType.Namespace == typeNamespace && attrib.AttributeType.Name == typeName
            select attrib).ToArray ();
    }

    public static PropertyDefinition? FindPropertyByName(Collection<PropertyDefinition> properties, string propertyName)
    {
        foreach (var property in properties)
        {
            if (property.Name == propertyName)
            {
                return property;
            }
        }

        return default;
    }

    public static Instruction CreateLoadInstructionOutParam(ParameterDefinition param, PropertyType paramTypeCode)
    {
        while (true)
        {
            switch (paramTypeCode)
            {
                case PropertyType.Enum:
                    var param1 = param;
                    param = null!;
                    paramTypeCode = GetPrimitiveTypeCode(param1.ParameterType.Resolve().GetEnumUnderlyingType());
                    continue;

                case PropertyType.Bool:
                case PropertyType.Int8:
                case PropertyType.Byte:
                    return Instruction.Create(OpCodes.Ldind_I1);

                case PropertyType.Int16:
                case PropertyType.UInt16:
                    return Instruction.Create(OpCodes.Ldind_I2);

                case PropertyType.Int:
                case PropertyType.UInt32:
                    return Instruction.Create(OpCodes.Ldind_I4);

                case PropertyType.Int64:
                case PropertyType.UInt64:
                    return Instruction.Create(OpCodes.Ldind_I8);

                case PropertyType.Float:
                    return Instruction.Create(OpCodes.Ldind_R4);

                case PropertyType.Double:
                    return Instruction.Create(OpCodes.Ldind_R8);

                case PropertyType.Struct:
                    return Instruction.Create(OpCodes.Ldobj, param.ParameterType.GetElementType());

                case PropertyType.LazyObject:
                case PropertyType.WeakObject:
                case PropertyType.SoftClass:
                case PropertyType.SoftObject:
                case PropertyType.Class:
                    return Instruction.Create(OpCodes.Ldobj, param.ParameterType.GetElementType());

                case PropertyType.Delegate:
                case PropertyType.MulticastInlineDelegate:
                case PropertyType.MulticastSparseDelegate:
                    // Delegate/multicast delegates in C# are implemented as classes, use Ldind_Ref
                    return Instruction.Create(OpCodes.Ldind_Ref);

                case PropertyType.InternalManagedFixedSizeArray:
                case PropertyType.InternalNativeFixedSizeArray:
                    throw new NotImplementedException(); // Fixed size arrays not supported as args

                case PropertyType.Array:
                case PropertyType.Set:
                case PropertyType.Map:
                    // Assumes this will be always be an object (IList, List, ISet, HashSet, IDictionary, Dictionary)
                    return Instruction.Create(OpCodes.Ldind_Ref);

                case PropertyType.Unknown:
                case PropertyType.Interface:
                case PropertyType.Object:
                case PropertyType.ObjectPtr:
                case PropertyType.Str:
                case PropertyType.Name:
                case PropertyType.Text:
                case PropertyType.DefaultComponent:
                default:
                    return Instruction.Create(OpCodes.Ldind_Ref);
            }
        }
    }

    public static Instruction CreateSetInstructionOutParam(ParameterDefinition param, PropertyType paramTypeCode)
    {
        while (true)
        {
            switch (paramTypeCode)
            {
                case PropertyType.Enum:
                    var param1 = param;
                    param = null;
                    paramTypeCode = GetPrimitiveTypeCode(param1.ParameterType.Resolve().GetEnumUnderlyingType());
                    continue;

                case PropertyType.Bool:
                case PropertyType.Int8:
                case PropertyType.Byte:
                    return Instruction.Create(OpCodes.Stind_I1);

                case PropertyType.Int16:
                case PropertyType.UInt16:
                    return Instruction.Create(OpCodes.Stind_I2);

                case PropertyType.Int:
                case PropertyType.UInt32:
                    return Instruction.Create(OpCodes.Stind_I4);

                case PropertyType.Int64:
                case PropertyType.UInt64:
                    return Instruction.Create(OpCodes.Stind_I8);

                case PropertyType.Float:
                    return Instruction.Create(OpCodes.Stind_R4);

                case PropertyType.Double:
                    return Instruction.Create(OpCodes.Stind_R8);

                case PropertyType.Struct:
                    return Instruction.Create(OpCodes.Stobj, param.ParameterType.GetElementType());

                case PropertyType.LazyObject:
                case PropertyType.WeakObject:
                case PropertyType.SoftClass:
                case PropertyType.SoftObject:
                case PropertyType.Class:
                case PropertyType.Name:
                case PropertyType.Text:
                    return Instruction.Create(OpCodes.Stobj, param.ParameterType.GetElementType());

                case PropertyType.Delegate:
                case PropertyType.MulticastSparseDelegate:
                case PropertyType.MulticastInlineDelegate:
                    // Delegate/multicast delegates in C# are implemented as classes, use Stind_Ref
                    return Instruction.Create(OpCodes.Stind_Ref);

                case PropertyType.InternalManagedFixedSizeArray:
                case PropertyType.InternalNativeFixedSizeArray:
                    throw new NotImplementedException(); // Fixed size arrays not supported as args

                case PropertyType.Array:
                case PropertyType.Set:
                case PropertyType.Map:
                    // Assumes this will be always be an object (IList, List, ISet, HashSet, IDictionary, Dictionary)
                    return Instruction.Create(OpCodes.Stind_Ref);

                case PropertyType.Unknown:
                case PropertyType.Interface:
                case PropertyType.Object:
                case PropertyType.ObjectPtr:
                case PropertyType.Str:
                case PropertyType.DefaultComponent:
                default:
                    return Instruction.Create(OpCodes.Stind_Ref);
            }
        }
    }

    public static PropertyType GetPrimitiveTypeCode(TypeReference type)
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
    
    public static bool HasAnyFlags(Enum flags, Enum testFlags)
    {
        return (Convert.ToUInt64(flags) & Convert.ToUInt64(testFlags)) != 0;
    }
}