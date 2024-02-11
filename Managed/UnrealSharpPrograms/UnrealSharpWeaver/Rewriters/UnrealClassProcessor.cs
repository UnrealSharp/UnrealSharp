using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.Rewriters;

public static class UnrealClassProcessor
{ 
    public static void ProcessClasses(IEnumerable<TypeDefinition> classes, ApiMetaData assemblyMetadata)
    {
        var typeToClassMetaData = classes.ToDictionary(type => type, type => new ClassMetaData(type));
        assemblyMetadata.ClassMetaData = typeToClassMetaData.Values.ToArray();

        var rewrittenClasses = new HashSet<TypeDefinition>();
        foreach (var classData in typeToClassMetaData)
        {
            ProcessParentClass(classData.Key, typeToClassMetaData, ref rewrittenClasses);
        }
    }
    
    private static void ProcessParentClass(TypeDefinition type, IReadOnlyDictionary<TypeDefinition, ClassMetaData> classDictionary, ref HashSet<TypeDefinition> rewrittenClasses)
    {
        var baseType = type.BaseType.Resolve();
        
        if (baseType != null && classDictionary.ContainsKey(baseType) && !rewrittenClasses.Contains(baseType))
        {
            ProcessParentClass(baseType, classDictionary, ref rewrittenClasses);
        }

        if (rewrittenClasses.Contains(type))
        {
            return;
        }
        
        ProcessClass(type, classDictionary[type]);
        rewrittenClasses.Add(type);
    }
    
    private static void ProcessClass(TypeDefinition classTypeDefinition, ClassMetaData metadata)
    {
        // Rewrite all the properties of the class to make getters/setters that call Native code.
        var offsetsToInitialize = new List<Tuple<FieldDefinition, PropertyMetaData>>();
        var pointersToInitialize = new List<Tuple<FieldDefinition, PropertyMetaData>>();
        PropertyRewriterHelpers.ProcessProperties(ref offsetsToInitialize, ref pointersToInitialize, classTypeDefinition, metadata.Properties);
        
        List<FunctionMetaData> functionsToRewrite = metadata.Functions.ToList();
        functionsToRewrite.AddRange(metadata.VirtualFunctions.Select(virtualFunction => virtualFunction));
        
        ProcessBlueprintOverrides(classTypeDefinition, metadata);
        
        // Add a field to cache the native UClass pointer.
        // Example: private static readonly nint NativeClassPtr = UCoreUObjectExporter.CallGetNativeClassFromName("MyActorClass");
        FieldDefinition nativeClassField = WeaverHelper.AddFieldToType(classTypeDefinition, "NativeClass", WeaverHelper.IntPtrType);
        
        ConstructorBuilder.CreateTypeInitializer(classTypeDefinition, Instruction.Create(OpCodes.Stsfld, nativeClassField), 
            [Instruction.Create(OpCodes.Call, WeaverHelper.GetNativeClassFromNameMethod)]);
        
        MethodDefinition staticConstructor = ConstructorBuilder.MakeStaticConstructor(classTypeDefinition);
        ILProcessor processor = staticConstructor.Body.GetILProcessor();
        Instruction loadNativeClassField = Instruction.Create(OpCodes.Ldsfld, nativeClassField);
        
        foreach (var function in metadata.Functions)
        {
            if (function.Parameters.Length == 0)
            {
                continue;
            }
            
            VariableDefinition variableDefinition = WeaverHelper.AddVariableToMethod(staticConstructor, WeaverHelper.IntPtrType);
            Instruction loadNativePointer = Instruction.Create(OpCodes.Ldloc, variableDefinition);
            Instruction storeNativePointer = Instruction.Create(OpCodes.Stloc, variableDefinition);
            
            function.EmitFunctionPointers(processor, loadNativeClassField, Instruction.Create(OpCodes.Stloc, variableDefinition));
            function.EmitFunctionParamOffsets(processor, loadNativePointer);
            function.EmitFunctionParamSize(processor, loadNativePointer);
            function.EmitParamElementSize(processor, loadNativePointer);
            
            foreach (var param in function.Parameters)
            {
                param.PropertyDataType.WritePostInitialization(processor, param, loadNativePointer, storeNativePointer);
            }
        }
        
        foreach (var property in metadata.Properties)
        {
            Instruction loadNativeProperty;
            Instruction setNativeProperty;
            if (property.NativePropertyField == null)
            {
                VariableDefinition nativePropertyVar = WeaverHelper.AddVariableToMethod(processor.Body.Method, WeaverHelper.IntPtrType);
                loadNativeProperty = Instruction.Create(OpCodes.Ldloc, nativePropertyVar);
                setNativeProperty = Instruction.Create(OpCodes.Stloc, nativePropertyVar);
            }
            else
            {
                loadNativeProperty = Instruction.Create(OpCodes.Ldsfld, property.NativePropertyField);
                setNativeProperty = Instruction.Create(OpCodes.Stsfld, property.NativePropertyField);
            }
            
            property.InitializePropertyPointers(processor, loadNativeClassField, setNativeProperty);
            property.InitializePropertyOffsets(processor, loadNativeProperty);
            property.PropertyDataType.WritePostInitialization(processor, property, loadNativeProperty, setNativeProperty);
        }
        
        processor.Emit(OpCodes.Ret);
    }

    private static void ProcessBlueprintOverrides(TypeDefinition classDefinition, ClassMetaData classMetaData)
    {
        foreach (MethodDefinition method in classMetaData.BlueprintEventOverrides)
        {
            var implementationMethodName = method.Name + "_Implementation";

            foreach (Instruction inst in method.Body.Instructions)
            {
                if (inst.OpCode != OpCodes.Call && inst.OpCode != OpCodes.Callvirt)
                {
                    continue;
                }
                
                MethodReference calledMethod = (MethodReference) inst.Operand;
                
                if (calledMethod.Name != method.Name)
                {
                    continue;
                }

                MethodReference? implementationMethod = WeaverHelper.FindMethod(classDefinition, implementationMethodName);
                inst.Operand = WeaverHelper.ImportMethod(implementationMethod);
                break;
            }
            
            method.Name = implementationMethodName;
        }
    }
}