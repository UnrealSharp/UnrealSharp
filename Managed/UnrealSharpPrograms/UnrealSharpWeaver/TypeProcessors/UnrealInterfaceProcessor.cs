using System.Collections.Immutable;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.TypeProcessors;

public static class UnrealInterfaceProcessor
{ 
    public static void ProcessInterfaces(List<TypeDefinition> interfaces, ApiMetaData assemblyMetadata)
    {
        assemblyMetadata.InterfacesMetaData.Capacity = interfaces.Count;
        
        for (var i = 0; i < interfaces.Count; ++i)
        {
            TypeDefinition interfaceType = interfaces[i];
            assemblyMetadata.InterfacesMetaData.Add(new InterfaceMetaData(interfaceType));
            
            CreateInterfaceWrapper(interfaceType);
            CreateInterfaceMarshaller(interfaceType);
        }
    }

    public static void CreateInterfaceWrapper(TypeDefinition interfaceType)
    {
        var unrealSharpObject = interfaceType.Module.ImportReference(WeaverImporter.Instance.UnrealSharpObjectType);
        TypeDefinition interfaceWrapperClass = WeaverImporter.Instance.UserAssembly.CreateNewClass(interfaceType.Namespace, interfaceType.GetWrapperClassName(), 
            TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.BeforeFieldInit,
            unrealSharpObject);
        interfaceWrapperClass.Interfaces.Add(new InterfaceImplementation(interfaceType));
        var importedIScriptInterface = interfaceType.Module.ImportReference(WeaverImporter.Instance.ScriptInterfaceWrapper);
        interfaceWrapperClass.Interfaces.Add(new InterfaceImplementation(importedIScriptInterface));

        // Add interface method implementations
        // For regular interface methods without implementation, throw InvalidOperationException
        interfaceWrapperClass.Module.ImportReference(typeof(InvalidOperationException));

        MethodReference exceptionCtor = interfaceWrapperClass.Module.ImportReference(
            typeof(InvalidOperationException).GetConstructor([typeof(string)]));
        
        foreach (var method in interfaceType.Methods)
        {
            // Skip property accessor methods
            if (method.IsGetter || method.IsSetter)
            {
                continue;
            }

            if (method.IsStatic)
            {
                continue;
            }

            // Check if the method is a UFunction
            bool isUFunction = method.IsUFunction();
            EFunctionFlags functionFlags = isUFunction ? method.GetFunctionFlags() : EFunctionFlags.None;
            bool isBlueprintEvent = isUFunction && functionFlags.HasFlag(EFunctionFlags.BlueprintNativeEvent);

            // Skip default implementation methods (they're handled by the interface implementation)
            if (method.HasBody && !isBlueprintEvent)
            {
                continue;
            }

            // Create method in the wrapper class
            MethodDefinition implementedMethod = new MethodDefinition(
                method.Name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final,
                method.ReturnType);

            // Copy method parameters
            foreach (var param in method.Parameters)
            {
                implementedMethod.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, param.ParameterType));
            }

            // Add method to the wrapper class
            interfaceWrapperClass.Methods.Add(implementedMethod);
        }

        foreach (var property in interfaceType.Properties)
        {
            if (property.GetMethod is { IsStatic: true } || property.SetMethod is { IsStatic: true })
            {
                continue;
            }
            
            var newProperty = new PropertyDefinition(property.Name, PropertyAttributes.None, property.PropertyType);
            interfaceWrapperClass.Properties.Add(newProperty);
            if (property.GetMethod != null)
            {
                var newGetMethod = new MethodDefinition($"get_{property.Name}", property.GetMethod.Attributes & ~MethodAttributes.Abstract | MethodAttributes.Final, property.GetMethod.ReturnType);
                newProperty.GetMethod = newGetMethod;
                interfaceWrapperClass.Methods.Add(newGetMethod);
            }
            
            if (property.SetMethod != null)
            {
                var newSetMethod = new MethodDefinition($"set{property.Name}", property.SetMethod.Attributes & ~MethodAttributes.Abstract | MethodAttributes.Final, WeaverImporter.Instance.VoidTypeRef);
                newSetMethod.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, property.PropertyType));
                newProperty.SetMethod = newSetMethod;
                interfaceWrapperClass.Methods.Add(newSetMethod);
            }
        }
        
        var originalMethods = interfaceWrapperClass.Methods.ToImmutableArray();
        var functionMetadata = new List<FunctionMetaData>();
        foreach (var newMethod in originalMethods)
        {
            // Add method body
            newMethod.Body = new MethodBody(newMethod);
            var ilProcessor = newMethod.Body.GetILProcessor();
    
            if (newMethod.ReturnType.FullName == "System.Void")
            {
                // For void methods, just return
                ilProcessor.Emit(OpCodes.Ret);
            }
            else
            {
                // For methods with return value, load default value and return
                if (newMethod.ReturnType.IsValueType)
                {
                    ilProcessor.Emit(OpCodes.Initobj, newMethod.ReturnType);  // For value types
                }
                else
                {
                    ilProcessor.Emit(OpCodes.Ldnull);  // For reference types
                }
                ilProcessor.Emit(OpCodes.Ret);
            }
            
           var metaData = new FunctionMetaData(newMethod, false, EFunctionFlags.BlueprintNativeEvent);
           functionMetadata.Add(metaData);
       }

        var (importedUObjectType, nativeObjectGetter) = CreateObjectBackingCode(interfaceType, interfaceWrapperClass);
        foreach (Instruction instruction in interfaceWrapperClass.Methods
                     .Where(method => method.Name != "get_NativeObject")
                     .SelectMany(method => method.Body.Instructions, (method, instruction) => (method, instruction))
                     .Where(t => t.instruction.OpCode == OpCodes.Call)
                     .Select(t => (t, methodRef: t.instruction.Operand as MethodReference))
                     .Where(t => t.methodRef?.FullName == WeaverImporter.Instance.NativeObjectGetter.FullName)
                     .Select(t => t.t.instruction))
        {
            instruction.Operand = nativeObjectGetter;
        }
        
        GenerateStaticConstructor(interfaceType, interfaceWrapperClass, functionMetadata);

        MethodDefinition wrapMethodDefinition = interfaceType.AddMethod("Wrap", interfaceType, 
            MethodAttributes.Public | MethodAttributes.Static, importedUObjectType);
        
        ILProcessor processor = wrapMethodDefinition.Body.GetILProcessor();

        // Create a constructor for the wrapper class that takes a UObject parameter
        MethodDefinition constructorMethod = ConstructorBuilder.CreateConstructor(interfaceWrapperClass, MethodAttributes.Public, importedUObjectType);
        ILProcessor constructorProcessor = constructorMethod.Body.GetILProcessor();

        // Call base constructor
        constructorProcessor.Emit(OpCodes.Ldarg_0);
        MethodReference objectCtorRef = interfaceWrapperClass.Module.ImportReference(typeof(object).GetConstructor(Type.EmptyTypes));
        constructorProcessor.Emit(OpCodes.Call, objectCtorRef);

        // Store UObject parameter in the backing field
        FieldDefinition objectBackingField = interfaceWrapperClass.Fields.First(f => f.Name == "<Object>k__BackingField");
        constructorProcessor.Emit(OpCodes.Ldarg_0);
        constructorProcessor.Emit(OpCodes.Ldarg_1);
        constructorProcessor.Emit(OpCodes.Stfld, objectBackingField);
        constructorProcessor.Emit(OpCodes.Ret);

        // In the Wrap method, create an instance of the wrapper class
        processor.Emit(OpCodes.Ldarg_0); // Load UObject parameter
        processor.Emit(OpCodes.Newobj, constructorMethod); // Create new wrapper instance with UObject parameter
        processor.Emit(OpCodes.Ret); // Return the wrapper instance

        wrapMethodDefinition.OptimizeMethod();
    }
    private static void GenerateStaticConstructor(TypeDefinition interfaceType, TypeDefinition interfaceWrapperClass, List<FunctionMetaData> functionMetadata)
    {
        FieldDefinition nativePointerField = interfaceWrapperClass.AddField("NativeInterfaceClassPtr", 
            WeaverImporter.Instance.IntPtrType, FieldAttributes.Public | FieldAttributes.Static);
        
        string interfaceName = interfaceType.GetEngineName();
        const bool finalizeMethod = true;
        
        ConstructorBuilder.CreateTypeInitializer(interfaceWrapperClass, Instruction.Create(OpCodes.Stsfld, nativePointerField), 
            [Instruction.Create(OpCodes.Call, WeaverImporter.Instance.GetNativeInterfaceFromNameMethod)], interfaceName, finalizeMethod);
        
        MethodDefinition staticConstructor = ConstructorBuilder.MakeStaticConstructor(interfaceWrapperClass);
        ILProcessor constructorProcessor = staticConstructor.Body.GetILProcessor();
        Instruction loadNativeClassField = Instruction.Create(OpCodes.Ldsfld, nativePointerField);

        foreach (var function in functionMetadata)
        {
            EmitFunctionGlueToStaticCtor(function, constructorProcessor, loadNativeClassField, staticConstructor);
        }
        
        constructorProcessor.Emit(OpCodes.Ret);
        staticConstructor.OptimizeMethod();
    }
    private static (TypeReference, MethodDefinition) CreateObjectBackingCode(TypeDefinition interfaceType, TypeDefinition interfaceWrapperClass)
    {
        // Import UObject type into the current module to ensure proper IL generation
        TypeReference importedUObjectType = interfaceType.Module.ImportReference(WeaverImporter.Instance.UObjectDefinition);

        // Add Object property with backing field
        // 1. Create backing field
        FieldDefinition objectBackingField = interfaceWrapperClass.AddField("<Object>k__BackingField", importedUObjectType, 
            FieldAttributes.Private);

        // 2. Create property definition
        PropertyDefinition objectProperty = new PropertyDefinition("Object", PropertyAttributes.None, importedUObjectType);
        interfaceWrapperClass.Properties.Add(objectProperty);

        // 3. Create getter method
        MethodDefinition objectGetter = new MethodDefinition("get_Object", 
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final, 
            importedUObjectType);
        interfaceWrapperClass.Methods.Add(objectGetter);

        // 4. Implement getter body (compiler generated)
        ILProcessor objectGetterProcessor = objectGetter.Body.GetILProcessor();
        objectGetterProcessor.Emit(OpCodes.Ldarg_0);
        objectGetterProcessor.Emit(OpCodes.Ldfld, objectBackingField);
        objectGetterProcessor.Emit(OpCodes.Ret);

        // 5. Associate getter with property
        objectProperty.GetMethod = objectGetter;

        // Add NativeObject property (without backing field, using Object.NativeObject)
        // 1. Create property definition
        PropertyDefinition nativeObjectProperty = new PropertyDefinition("NativeObject", PropertyAttributes.None, 
            WeaverImporter.Instance.IntPtrType);
        interfaceWrapperClass.Properties.Add(nativeObjectProperty);

        // 2. Create getter method
        MethodDefinition nativeObjectGetter = new MethodDefinition("get_NativeObject", 
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName, 
            WeaverImporter.Instance.IntPtrType);
        interfaceWrapperClass.Methods.Add(nativeObjectGetter);

        // 3. Implement getter body that calls to Object.NativeObject
        ILProcessor nativeObjectGetterProcessor = nativeObjectGetter.Body.GetILProcessor();
        nativeObjectGetterProcessor.Emit(OpCodes.Ldarg_0); // this
        nativeObjectGetterProcessor.Emit(OpCodes.Call, objectGetter); // get_Object()
        nativeObjectGetterProcessor.Emit(OpCodes.Call, WeaverImporter.Instance.NativeObjectGetter); // .NativeObject
        nativeObjectGetterProcessor.Emit(OpCodes.Ret);

        // 4. Associate getter with property
        nativeObjectProperty.GetMethod = nativeObjectGetter;

        // Add IL code to the getter to call Object.NativeObject
        ILProcessor getterProcessor = nativeObjectGetter.Body.GetILProcessor();
        getterProcessor.Emit(OpCodes.Ldarg_0);
        getterProcessor.Emit(OpCodes.Call, objectProperty.GetMethod);
        getterProcessor.Emit(OpCodes.Call, WeaverImporter.Instance.NativeObjectGetter);
        getterProcessor.Emit(OpCodes.Ret);
        nativeObjectGetter.OptimizeMethod();
        return (importedUObjectType, nativeObjectGetter);
    }

    public static void CreateInterfaceMarshaller(TypeDefinition interfaceType)
    {
        TypeDefinition structMarshallerClass = WeaverImporter.Instance.UserAssembly.CreateNewClass(interfaceType.Namespace, interfaceType.GetMarshallerClassName(), 
            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.BeforeFieldInit);
        
        FieldDefinition nativePointerField = structMarshallerClass.AddField("NativeInterfaceClassPtr", 
            WeaverImporter.Instance.IntPtrType, FieldAttributes.Public | FieldAttributes.Static);
        
        string interfaceName = interfaceType.GetEngineName();
        const bool finalizeMethod = true;
        
        ConstructorBuilder.CreateTypeInitializer(structMarshallerClass, Instruction.Create(OpCodes.Stsfld, nativePointerField), 
            [Instruction.Create(OpCodes.Call, WeaverImporter.Instance.GetNativeInterfaceFromNameMethod)], interfaceName, finalizeMethod);
        
        MakeToNativeMethod(interfaceType, structMarshallerClass, nativePointerField);
        MakeFromNativeMethod(interfaceType, structMarshallerClass, nativePointerField);
    }
    
    public static void MakeToNativeMethod(TypeDefinition interfaceType, TypeDefinition structMarshallerClass, FieldDefinition nativePointerField)
    {
        MethodDefinition toNativeMarshallerMethod = structMarshallerClass.AddMethod("ToNative", 
            WeaverImporter.Instance.VoidTypeRef,
            MethodAttributes.Public | MethodAttributes.Static, WeaverImporter.Instance.IntPtrType, WeaverImporter.Instance.Int32TypeRef, interfaceType);
        
        MethodReference toNativeMethod = WeaverImporter.Instance.ScriptInterfaceMarshaller.FindMethod("ToNative")!;
        toNativeMethod = FunctionProcessor.MakeMethodDeclaringTypeGeneric(toNativeMethod, interfaceType);
        
        ILProcessor toNativeMarshallerProcessor = toNativeMarshallerMethod.Body.GetILProcessor();
        toNativeMarshallerProcessor.Emit(OpCodes.Ldarg_0);
        toNativeMarshallerProcessor.Emit(OpCodes.Ldarg_1);
        toNativeMarshallerProcessor.Emit(OpCodes.Ldarg_2);
        toNativeMarshallerProcessor.Emit(OpCodes.Ldsfld, nativePointerField);
        toNativeMarshallerProcessor.Emit(OpCodes.Call, toNativeMethod);
        
        toNativeMarshallerMethod.FinalizeMethod();
    }
    
    public static void MakeFromNativeMethod(TypeDefinition interfaceType, TypeDefinition structMarshallerClass, FieldDefinition nativePointerField)
    {
        MethodDefinition fromNativeMarshallerMethod = structMarshallerClass.AddMethod("FromNative", 
            interfaceType,
            MethodAttributes.Public | MethodAttributes.Static,
            [WeaverImporter.Instance.IntPtrType, WeaverImporter.Instance.Int32TypeRef]);
        
        MethodReference fromNativeMethod = WeaverImporter.Instance.ScriptInterfaceMarshaller.FindMethod("FromNative")!;
        fromNativeMethod = FunctionProcessor.MakeMethodDeclaringTypeGeneric(fromNativeMethod, interfaceType);
        
        ILProcessor fromNativeMarshallerProcessor = fromNativeMarshallerMethod.Body.GetILProcessor();
        fromNativeMarshallerProcessor.Emit(OpCodes.Ldarg_0);
        fromNativeMarshallerProcessor.Emit(OpCodes.Ldarg_1);
        fromNativeMarshallerProcessor.Emit(OpCodes.Call, fromNativeMethod);
        fromNativeMarshallerProcessor.Emit(OpCodes.Ret);
        fromNativeMarshallerMethod.OptimizeMethod();
    }
    
    static void EmitFunctionGlueToStaticCtor(FunctionMetaData function, ILProcessor processor, Instruction loadNativeClassField, MethodDefinition staticConstructor)
    {
        try
        {
            if (!function.HasParameters)
            {
                return;
            }
            
            VariableDefinition variableDefinition = staticConstructor.AddLocalVariable(WeaverImporter.Instance.IntPtrType);
            Instruction loadNativePointer = Instruction.Create(OpCodes.Ldloc, variableDefinition);
            Instruction storeNativePointer = Instruction.Create(OpCodes.Stloc, variableDefinition);
            
            function.EmitFunctionPointers(processor, loadNativeClassField, Instruction.Create(OpCodes.Stloc, variableDefinition));
            function.EmitFunctionParamOffsets(processor, loadNativePointer);
            function.EmitFunctionParamSize(processor, loadNativePointer);
            function.EmitParamNativeProperty(processor, loadNativePointer);
            
            foreach (var param in function.Parameters)
            {
                param.PropertyDataType.WritePostInitialization(processor, param, loadNativePointer, storeNativePointer);
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to emit function glue for {function.Name}", e);
        }
    }
}