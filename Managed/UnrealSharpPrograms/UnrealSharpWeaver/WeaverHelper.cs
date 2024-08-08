using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using UnrealSharpWeaver.NativeTypes;

namespace UnrealSharpWeaver;

public static class WeaverHelper
{
    public static readonly string UnrealSharpNamespace = "UnrealSharp";
    public static readonly string InteropNameSpace = UnrealSharpNamespace + ".Interop";
    public static readonly string AttributeNamespace = UnrealSharpNamespace + ".Attributes";
    public static readonly string CoreUObjectNamespace = UnrealSharpNamespace + ".CoreUObject";
    
    public static readonly string UnrealSharpObject = "UnrealSharpObject";
    public static readonly string FPropertyCallbacks = "FPropertyExporter";
    public static readonly string UClassCallbacks = "UClassExporter";
    public static readonly string CoreUObjectCallbacks = "UCoreUObjectExporter";
    public static readonly string FBoolPropertyCallbacks = "FBoolPropertyExporter";
    public static readonly string FStringCallbacks = "FStringExporter";
    public static readonly string UObjectCallbacks = "UObjectExporter";
    public static readonly string FArrayPropertyCallbacks = "FArrayPropertyExporter";
    public static readonly string UScriptStructCallbacks = "UScriptStructExporter";
    public static readonly string UFunctionCallbacks = "UFunctionExporter";
    public static readonly string MulticastDelegatePropertyCallbacks = "FMulticastDelegatePropertyExporter";
    public static readonly string UStructCallbacks = "UStructExporter";
    public static readonly string MarshallerSuffix = "Marshaller";
    
    public static readonly string UPropertyAttribute = "UPropertyAttribute";
    public static readonly string UMetaDataAttribute = "UMetaDataAttribute";
    public static readonly string UEnumAttribute = "UEnumAttribute";
    public static readonly string UStructAttribute = "UStructAttribute";
    public static readonly string UFunctionAttribute = "UFunctionAttribute";
    public static readonly string UClassAttribute = "UClassAttribute";
    public static readonly string UInterfaceAttribute = "UInterfaceAttribute";
    
    public static readonly string GeneratedTypeAttribute = "GeneratedTypeAttribute";
    public static readonly string BlittableTypeAttribute = "BlittableTypeAttribute";
    
    public static AssemblyDefinition UserAssembly;
    public static AssemblyDefinition BindingsAssembly;
    public static MethodReference NativeObjectGetter;
    public static TypeDefinition IntPtrType;
    public static MethodReference IntPtrAdd;
    public static FieldReference IntPtrZero;
    public static MethodReference IntPtrEqualsOperator;
    public static TypeReference UnrealSharpObjectType;
    public static TypeDefinition IInterfaceType;
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
    public static MethodReference GetNativePropertyFromNameMethod;
    public static MethodReference GetNativeFunctionFromClassAndNameMethod;
    public static MethodReference GetNativeFunctionParamsSizeMethod;
    public static MethodReference GetNativeStructSizeMethod;
    public static MethodReference InvokeNativeFunctionMethod;
    public static MethodReference GetSignatureFunction;
    public static MethodReference InitializeStructMethod;

    public static MethodReference GeneratedTypeCtor;
    
    public static TypeDefinition UObjectDefinition;
    
    public static MethodReference BlittableTypeConstructor;
    
    private static readonly MethodAttributes MethodAttributes = MethodAttributes.Public | MethodAttributes.Static;
    
    public static void Initialize(AssemblyDefinition bindingsAssembly)
    {
        BindingsAssembly = bindingsAssembly;
    }
    
    public static void ForEachAssembly(Func<AssemblyDefinition, bool> action)
    {
        List<AssemblyDefinition> assemblies = [BindingsAssembly, UserAssembly];
        foreach (var assembly in assemblies)
        {
            if (!action(assembly))
            {
                return;
            }
        }
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

        UnrealSharpObjectType = FindTypeInAssembly(BindingsAssembly, UnrealSharpObject, UnrealSharpNamespace)!;
        IInterfaceType = FindTypeInAssembly(BindingsAssembly, "IInterface", CoreUObjectNamespace)!.Resolve();
        
        TypeDefinition unrealSharpObjectType = UnrealSharpObjectType.Resolve();
        NativeObjectGetter = FindMethod(unrealSharpObjectType, "get_NativeObject")!;

        GetNativeFunctionFromInstanceAndNameMethod = FindExporterMethod(UClassCallbacks, "CallGetNativeFunctionFromInstanceAndName");
        GetNativeStructFromNameMethod = FindExporterMethod(CoreUObjectCallbacks, "CallGetNativeStructFromName");
        GetNativeClassFromNameMethod = FindExporterMethod(CoreUObjectCallbacks, "CallGetNativeClassFromName");
        GetPropertyOffsetFromNameMethod = FindExporterMethod(FPropertyCallbacks, "CallGetPropertyOffsetFromName");
        GetPropertyOffset = FindExporterMethod(FPropertyCallbacks, "CallGetPropertyOffset");
        GetNativePropertyFromNameMethod = FindExporterMethod(FPropertyCallbacks, "CallGetNativePropertyFromName");
        GetNativeFunctionFromClassAndNameMethod = FindExporterMethod(UClassCallbacks, "CallGetNativeFunctionFromClassAndName");
        GetNativeFunctionParamsSizeMethod = FindExporterMethod(UFunctionCallbacks, "CallGetNativeFunctionParamsSize");
        GetNativeStructSizeMethod = FindExporterMethod(UScriptStructCallbacks, "CallGetNativeStructSize");
        InvokeNativeFunctionMethod = FindExporterMethod(UObjectCallbacks, "CallInvokeNativeFunction");
        GetSignatureFunction = FindExporterMethod(MulticastDelegatePropertyCallbacks, "CallGetSignatureFunction");
        InitializeStructMethod = FindExporterMethod(UStructCallbacks, "CallInitializeStruct");
        
        UObjectDefinition = FindTypeInAssembly(BindingsAssembly, "UObject", CoreUObjectNamespace)!.Resolve();
        
        TypeReference blittableType = FindTypeInAssembly(BindingsAssembly, BlittableTypeAttribute, AttributeNamespace)!;
        BlittableTypeConstructor = FindMethod(blittableType.Resolve(), ".ctor")!;

        TypeReference generatedType = FindTypeInAssembly(BindingsAssembly, GeneratedTypeAttribute, AttributeNamespace)!;
        GeneratedTypeCtor = FindMethod(generatedType.Resolve(), ".ctor")!;
    }
    
    public static TypeReference? FindGenericTypeInAssembly(AssemblyDefinition assembly, string typeNamespace, string typeName, TypeReference[] typeParameters, bool bThrowOnException = true)
    {
        TypeReference? typeRef = FindTypeInAssembly(assembly, typeName, typeNamespace, bThrowOnException);
        return typeRef == null ? null : UserAssembly.MainModule.ImportReference(typeRef.Resolve().MakeGenericInstanceType(typeParameters));
    }

    public static TypeReference? FindTypeInAssembly(AssemblyDefinition assembly, string typeName, string typeNamespace = "", bool throwOnException = true)
    {
        foreach (var module in assembly.Modules)
        {
            foreach (var type in module.GetAllTypes())
            {
                if ((typeNamespace.Length > 0 && type.Namespace != typeNamespace) || type.Name != typeName)
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
        return FindBindingsStaticMethod(InteropNameSpace, exporterName, functionName);
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

    public static MethodDefinition CopyMethod(string name, MethodDefinition method, bool addMethod = true)
    {
        MethodDefinition newMethod = new MethodDefinition(name, method.Attributes, method.ReturnType)
        {
            HasThis = true,
            ExplicitThis = method.ExplicitThis,
            CallingConvention = method.CallingConvention,
            Body = method.Body
        };

        foreach (ParameterDefinition parameter in method.Parameters)
        {
            TypeReference importedType = ImportType(parameter.ParameterType);
            newMethod.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, importedType));
        }
        
        if (addMethod)
        {
            method.DeclaringType.Methods.Add(newMethod);
        }

        return newMethod;
    }
    
    public static string GetInvokeName(string methodName)
    {
        return "Invoke_" + methodName;
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

    public static bool HasMethod(TypeDefinition typeDef, string methodName, bool throwIfNotFound = true, params TypeReference[] parameterTypes)
    {
        return FindMethod(typeDef, methodName, throwIfNotFound, parameterTypes) != null;
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
    
    public static void AddGeneratedTypeAttribute(TypeDefinition type)
    {
        CustomAttribute attribute = new CustomAttribute(GeneratedTypeCtor);
        string typeName = type.Name.Substring(1);
        string fullTypeName = type.Namespace + "." + typeName;
        attribute.ConstructorArguments.Add(new CustomAttributeArgument(UserAssembly.MainModule.TypeSystem.String, typeName));
        attribute.ConstructorArguments.Add(new CustomAttributeArgument(UserAssembly.MainModule.TypeSystem.String, fullTypeName));
        
        type.CustomAttributes.Add(attribute);
    }

    public static string GetEngineName(IMemberDefinition memberDefinition)
    {
        IMemberDefinition currentMemberIteration = memberDefinition;
        while (currentMemberIteration != null)
        {
            foreach (var customAttribute in currentMemberIteration.CustomAttributes)
            {
                if (customAttribute.AttributeType.Name != GeneratedTypeAttribute)
                {
                    continue;
                }
                
                return (string) customAttribute.ConstructorArguments[0].Value;
            }
            
            if (currentMemberIteration is MethodDefinition methodDefinition && methodDefinition.IsVirtual)
            {
                if (currentMemberIteration == methodDefinition.GetBaseMethod())
                {
                    break;
                }
                
                currentMemberIteration = methodDefinition.GetBaseMethod();
            }
            else
            {
                break;
            }
        }
        
        // Same name in engine as in managed code
        return memberDefinition.Name;
    }
    
    public static void FinalizeMethod(MethodDefinition method)
    {
        method.Body.GetILProcessor().Emit(OpCodes.Ret);
        OptimizeMethod(method);
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
            parameters = [IntPtrType, Int32TypeRef, valueType];
        }
        
        MethodDefinition toNativeMethod = AddMethodToType(type, "ToNative", VoidTypeRef, MethodAttributes, parameters);
        return toNativeMethod;
    }
    
    public static MethodDefinition AddFromNativeMethod(TypeDefinition type, TypeDefinition returnType, TypeReference[]? parameters = null)
    {
        if (parameters == null)
        {
            parameters = [IntPtrType, Int32TypeRef];
        }
        
        MethodDefinition fromNative = AddMethodToType(type, "FromNative", returnType, MethodAttributes, parameters);
        return fromNative;
    }
    
    public static NativeDataType GetDataType(TypeReference typeRef, string propertyName, Collection<CustomAttribute>? customAttributes)
    {
        int arrayDim = 1;
        TypeDefinition typeDef = typeRef.Resolve();
        SequencePoint sequencePoint = ErrorEmitter.GetSequencePointFromMemberDefinition(typeDef);

        if (customAttributes != null)
        {
            CustomAttribute? propertyAttribute = GetUProperty(typeDef);
            
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

                if (typeRef.IsGenericInstance)
                {
                    GenericInstanceType GenericType = (GenericInstanceType)typeRef;
                    var GenericTypeName = GenericType.Name;
                    TypeReference innerType = GenericType.GenericArguments[0];
                    
                    if (GenericTypeName.Contains("TArray`1") || GenericTypeName.Contains("List`1"))
                    {
                        return new NativeDataArrayType(typeRef, arrayDim, innerType);
                    }
                    
                    if (GenericTypeName.Contains("TMap`2") || GenericTypeName.Contains("Dictionary`2"))
                    {
                        return new NativeDataMapType(typeRef, arrayDim, innerType, GenericType.GenericArguments[1]);
                    }

                    if (GenericTypeName.Contains("TSubclassOf`1"))
                    {
                        return new NativeDataClassType(typeRef, innerType, arrayDim);
                    }

                    if (GenericTypeName.Contains("TWeakObjectPtr`1"))
                    {
                        return new NativeDataWeakObjectType(typeRef, innerType, arrayDim);
                    }

                    if (GenericTypeName.Contains("TSoftObjectPtr`1"))
                    {
                        return new NativeDataSoftObjectType(typeRef, innerType, arrayDim);
                    }

                    if (GenericTypeName.Contains("TSoftClassPtr`1"))
                    {
                        return new NativeDataSoftClassType(typeRef, innerType, arrayDim);
                    }
                }

                if (typeDef.IsEnum)
                {
                    CustomAttribute? enumAttribute = GetUEnum(typeDef);
                
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

                if (!typeDef.IsClass)
                {
                    throw new InvalidPropertyException(propertyName, sequencePoint, "No Unreal type for " + typeRef.FullName);
                }
                
                if (typeDef.FullName == "UnrealSharp.FText")
                {
                    return new NativeDataTextType(typeDef);
                }
                
                if (typeDef.FullName == "UnrealSharp.FName")
                {
                    return new NativeDataNameType(typeDef, arrayDim);
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
                CustomAttribute? structAttribute = GetUStruct(typeDef);
                
                if (structAttribute == null)
                {
                    throw new Exception("Structs must have a UStruct attribute if exposed to Unreal Engine: " + typeDef.FullName);
                }
                
                return GetBlittableType(typeDef) != null ? new NativeDataBlittableStructType(typeDef, arrayDim) : new NativeDataStructType(typeDef, GetMarshallerClassName(typeDef), arrayDim);
        }
    }
    
    public static string GetMarshallerClassName(TypeReference typeRef)
    {
        return typeRef.Name + "Marshaller";
    }
    
    public static CustomAttribute?[] FindMetaDataAttributes(IEnumerable<CustomAttribute> customAttributes)
    {
        return FindAttributesByType(customAttributes, AttributeNamespace, UMetaDataAttribute);
    }

    public static CustomAttributeArgument? FindAttributeField(CustomAttribute attribute, string fieldName)
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
    
    public static bool MethodIsCompilerGenerated(ICustomAttributeProvider method)
    {
        return FindAttributeByType(method.CustomAttributes, "System.Runtime.CompilerServices", "CompilerGeneratedAttribute") != null;
    }
    
    public static CustomAttribute? FindAttributeByType(IEnumerable<CustomAttribute> customAttributes, string typeNamespace, string typeName)
    {
        CustomAttribute?[] attribs = FindAttributesByType(customAttributes, typeNamespace, typeName);
        return attribs.Length == 0 ? null : attribs[0];
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
                case PropertyType.String:
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
                case PropertyType.String:
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
    
    public static CustomAttribute? FindAttribute(Collection<CustomAttribute> customAttributes, string attributeName)
    {
        return FindAttributeByType(customAttributes, AttributeNamespace, attributeName);
    }
    
    public static CustomAttribute? GetUProperty(Collection<CustomAttribute> attributes)
    {
        return FindAttribute(attributes, UPropertyAttribute);
    }
    
    public static CustomAttribute? GetBlittableType(TypeDefinition type)
    {
        return FindAttribute(type.CustomAttributes, BlittableTypeAttribute);
    }
    
    public static CustomAttribute? GetUProperty(IMemberDefinition property)
    {
        return FindAttribute(property.CustomAttributes, UPropertyAttribute);
    }
    
    public static CustomAttribute? GetUFunction(MethodDefinition function)
    {
        return FindAttribute(function.CustomAttributes, UFunctionAttribute);
    }
    
    public static CustomAttribute? GetUClass(TypeDefinition type)
    {
        return FindAttribute(type.CustomAttributes, UClassAttribute);
    }
    
    public static CustomAttribute? GetUEnum(TypeDefinition type)
    {
        return FindAttribute(type.CustomAttributes, UEnumAttribute);
    }
    
    public static CustomAttribute? GetUStruct(TypeDefinition type)
    {
        return FindAttribute(type.CustomAttributes, UStructAttribute);
    }
    
    public static CustomAttribute? GetUInterface(TypeDefinition type)
    {
        return FindAttribute(type.CustomAttributes, UInterfaceAttribute);
    }
        
    public static bool IsUProperty(IMemberDefinition property)
    {
        return GetUProperty(property) != null;
    }
    
    public static bool IsUInterface(TypeDefinition typeDefinition)
    {
        return GetUInterface(typeDefinition) != null;
    }
    
    public static bool IsUClass(TypeDefinition typeDefinition)
    {
        return GetUClass(typeDefinition) != null;
    }
    
    public static bool IsGenerated(TypeDefinition typeDefinition)
    {
        return FindAttribute(typeDefinition.CustomAttributes, GeneratedTypeAttribute) != null;
    }
    
    public static bool IsValidBaseForUObject(TypeDefinition typeDefinition)
    {
        if (!WeaverHelper.IsUClass(typeDefinition))
        {
            return false;
        }
        
        while (typeDefinition != null)
        {
            if (typeDefinition.BaseType == null)
            {
                return false;
            }
            
            if (typeDefinition == UObjectDefinition)
            {
                return true;
            }

            typeDefinition = typeDefinition.BaseType.Resolve();
        }
        
        return false;
    }
    
    public static bool IsUEnum(TypeDefinition typeDefinition)
    {
        return GetUEnum(typeDefinition) != null;
    }
    
    public static bool IsUStruct(TypeDefinition typeDefinition)
    {
        return GetUStruct(typeDefinition) != null;
    }
    
    public static bool IsUFunction(MethodDefinition method)
    {
        return GetUFunction(method) != null;
    }
}