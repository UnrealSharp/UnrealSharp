using Mono.Cecil;
using Mono.Cecil.Cil;
using UnrealSharpWeaver.MetaData;

namespace UnrealSharpWeaver.TypeProcessors;

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
        foreach (var field in classTypeDefinition.Fields)
        {
            if (WeaverHelper.IsUProperty(field))
            {
                throw new InvalidPropertyException(field, "Fields cannot be UProperty");
            }
        }
        
        // Rewrite all the properties of the class to make getters/setters that call Native code.
        if (classTypeDefinition.Properties.Count > 0)
        {
            var offsetsToInitialize = new List<Tuple<FieldDefinition, PropertyMetaData>>();
            var pointersToInitialize = new List<Tuple<FieldDefinition, PropertyMetaData>>();
            PropertyRewriterHelpers.ProcessClassMembers(ref offsetsToInitialize, ref pointersToInitialize, classTypeDefinition, metadata.Properties);
        }
        
        ProcessBlueprintOverrides(classTypeDefinition, metadata);
        
        // Add a field to cache the native UClass pointer.
        FieldDefinition nativeClassField = WeaverHelper.AddFieldToType(classTypeDefinition, "NativeClass", WeaverHelper.IntPtrType);
        
        // private static readonly nint NativeClassPtr = UCoreUObjectExporter.CallGetNativeClassFromName("MyActorClass");
        ConstructorBuilder.CreateTypeInitializer(classTypeDefinition, Instruction.Create(OpCodes.Stsfld, nativeClassField), 
            [Instruction.Create(OpCodes.Call, WeaverHelper.GetNativeClassFromNameMethod)]);
        
        MethodDefinition staticConstructor = ConstructorBuilder.MakeStaticConstructor(classTypeDefinition);
        ILProcessor processor = staticConstructor.Body.GetILProcessor();
        Instruction loadNativeClassField = Instruction.Create(OpCodes.Ldsfld, nativeClassField);

        if (classTypeDefinition.Properties.Count > 0)
        {
            ConstructorBuilder.InitializeFields(staticConstructor, metadata.Properties, loadNativeClassField);
        }
        
        foreach (var function in metadata.Functions)
        {
            if (!function.HasParameters())
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
        
        WeaverHelper.FinalizeMethod(staticConstructor);
    }

    private static void ProcessBlueprintOverrides(TypeDefinition classDefinition, ClassMetaData classMetaData)
    {
        foreach (FunctionMetaData functionMetaData in classMetaData.Functions)
        {
            MethodDefinition method = functionMetaData.MethodDefinition;
            var implementationMethodName = method.Name + "_Implementation";
            MethodReference? ownImplementationMethod = WeaverHelper.FindOwnMethod(classDefinition, implementationMethodName, throwIfNotFound: false);
            
            if (ownImplementationMethod != null)
            {
                // An implementation method was already generated by a previous step so we don't need to do it here
                continue;
            }

            // Change any calls to the base method to instead call the implementation method
            foreach (var inst in method.Body.Instructions)
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

                // TODO: We should probably check that the target of the call is actually the base class.
                // Otherwise we might end up changing calls to unrelated methods on other objects with the same name
                MethodReference implementationMethod = WeaverHelper.FindMethod(classDefinition, implementationMethodName)!;
                inst.Operand = WeaverHelper.ImportMethod(implementationMethod);
                break;
            }
            
            method.Name = implementationMethodName;
        }
    }
}