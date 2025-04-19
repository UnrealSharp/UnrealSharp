using Mono.Cecil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver;

public static class WeaverImporter
{
    private static readonly string Attributes = ".Attributes";
    
    public static readonly string UnrealSharpNamespace = "UnrealSharp";
    public static readonly string UnrealSharpAttributesNamespace = UnrealSharpNamespace + Attributes;
    
    public static readonly string UnrealSharpCoreNamespace = UnrealSharpNamespace + ".Core";
    public static readonly string UnrealSharpCoreAttributesNamespace = UnrealSharpCoreNamespace + Attributes;
    public static readonly string UnrealSharpCoreMarshallers = UnrealSharpCoreNamespace + ".Marshallers";
    
    public static readonly string InteropNameSpace = UnrealSharpNamespace + ".Interop";
    public static readonly string AttributeNamespace = UnrealSharpNamespace + Attributes;
    public static readonly string CoreUObjectNamespace = UnrealSharpNamespace + ".CoreUObject";
    public static readonly string EngineNamespace = UnrealSharpNamespace + ".Engine";
    
    public static readonly string UnrealSharpObject = "UnrealSharpObject";
    public static readonly string FPropertyCallbacks = "FPropertyExporter";

    public static readonly string CoreUObjectCallbacks = "UCoreUObjectExporter";
    public static readonly string UObjectCallbacks = "UObjectExporter";
    public static readonly string UScriptStructCallbacks = "UScriptStructExporter";
    public static readonly string UFunctionCallbacks = "UFunctionExporter";
    public static readonly string MulticastDelegatePropertyCallbacks = "FMulticastDelegatePropertyExporter";
    public static readonly string UStructCallbacks = "UStructExporter";
    
    public static readonly string GeneratedTypeAttribute = "GeneratedTypeAttribute";
    
    public static AssemblyDefinition UserAssembly = null!;
    public static readonly ICollection<AssemblyDefinition> WeavedAssemblies = [];
    
    public static AssemblyDefinition UnrealSharpAssembly => FindAssembly(UnrealSharpNamespace);
    public static AssemblyDefinition UnrealSharpCoreAssembly => FindAssembly(UnrealSharpNamespace + ".Core");
    public static AssemblyDefinition ProjectGlueAssembly => FindAssembly("ProjectGlue");
    
    public static MethodReference NativeObjectGetter = null!;
    public static TypeDefinition IntPtrType = null!;
    public static MethodReference IntPtrAdd = null!;
    public static FieldReference IntPtrZero = null!;
    public static MethodReference IntPtrEqualsOperator = null!;
    public static TypeReference UnrealSharpObjectType = null!;
    public static TypeDefinition IInterfaceType = null!;
    public static MethodReference GetNativeFunctionFromInstanceAndNameMethod = null!;
    public static TypeReference Int32TypeRef = null!;
    public static TypeReference VoidTypeRef = null!;
    public static TypeReference ByteTypeRef = null!;
    public static MethodReference GetNativeClassFromNameMethod = null!;
    public static MethodReference GetNativeStructFromNameMethod = null!;
    public static MethodReference GetPropertyOffsetFromNameMethod = null!;
    public static MethodReference GetPropertyOffset = null!;
    public static MethodReference GetNativePropertyFromNameMethod = null!;
    public static MethodReference GetNativeFunctionFromClassAndNameMethod = null!;
    public static MethodReference GetNativeFunctionParamsSizeMethod = null!;
    public static MethodReference GetNativeStructSizeMethod = null!;
    public static MethodReference InvokeNativeFunctionMethod = null!;
    public static MethodReference GetSignatureFunction = null!;
    public static MethodReference InitializeStructMethod = null!;

    public static MethodReference GeneratedTypeCtor = null!;
    
    public static TypeDefinition UObjectDefinition = null!;
    public static TypeDefinition UActorComponentDefinition = null!;
    
    public static TypeDefinition ScriptInterfaceMarshaller = null!;
    
    public static MethodReference BlittableTypeConstructor = null!;
    
    public static MethodReference GetAssemblyNameMethod = null!;
    public static MethodReference GetTypeFromHandleMethod = null!;

    public static DefaultAssemblyResolver AssemblyResolver = null!;
    
    public static void Initialize(DefaultAssemblyResolver assemblyResolver)
    {
        AssemblyResolver = assemblyResolver;
    }
    
    static AssemblyDefinition FindAssembly(string assemblyName)
    {
        return AssemblyResolver.Resolve(new AssemblyNameReference(assemblyName, new Version(0, 0)));
    }

    public static void ImportCommonTypes(AssemblyDefinition userAssembly)
    {
        UserAssembly = userAssembly;
        
        TypeSystem typeSystem = UserAssembly.MainModule.TypeSystem;
        
        Int32TypeRef = typeSystem.Int32;
        VoidTypeRef = typeSystem.Void;
        ByteTypeRef = typeSystem.Byte;
        
        IntPtrType = typeSystem.IntPtr.Resolve();
        IntPtrAdd = IntPtrType.FindMethod("Add")!;
        IntPtrZero = IntPtrType.FindField("Zero");
        IntPtrEqualsOperator = IntPtrType.FindMethod("op_Equality")!;

        UnrealSharpObjectType = UnrealSharpCoreAssembly.FindType(UnrealSharpObject, UnrealSharpCoreNamespace)!;
        IInterfaceType = UnrealSharpAssembly.FindType("IInterface", CoreUObjectNamespace)!.Resolve();
        
        TypeDefinition unrealSharpObjectType = UnrealSharpObjectType.Resolve();
        NativeObjectGetter = unrealSharpObjectType.FindMethod("get_NativeObject")!;

        GetNativeFunctionFromInstanceAndNameMethod = FindExporterMethod(TypeDefinitionUtilities.UClassCallbacks, "CallGetNativeFunctionFromInstanceAndName");
        
        GetNativeStructFromNameMethod = FindExporterMethod(CoreUObjectCallbacks, "CallGetNativeStructFromName");
        GetNativeClassFromNameMethod = FindExporterMethod(CoreUObjectCallbacks, "CallGetNativeClassFromName");
        
        GetPropertyOffsetFromNameMethod = FindExporterMethod(FPropertyCallbacks, "CallGetPropertyOffsetFromName");
        GetPropertyOffset = FindExporterMethod(FPropertyCallbacks, "CallGetPropertyOffset");
        
        GetNativePropertyFromNameMethod = FindExporterMethod(FPropertyCallbacks, "CallGetNativePropertyFromName");
        
        GetNativeFunctionFromClassAndNameMethod = FindExporterMethod(TypeDefinitionUtilities.UClassCallbacks, "CallGetNativeFunctionFromClassAndName");
        GetNativeFunctionParamsSizeMethod = FindExporterMethod(UFunctionCallbacks, "CallGetNativeFunctionParamsSize");
        
        GetNativeStructSizeMethod = FindExporterMethod(UScriptStructCallbacks, "CallGetNativeStructSize");
        
        InvokeNativeFunctionMethod = FindExporterMethod(UObjectCallbacks, "CallInvokeNativeFunction");
        
        GetSignatureFunction = FindExporterMethod(MulticastDelegatePropertyCallbacks, "CallGetSignatureFunction");
        
        InitializeStructMethod = FindExporterMethod(UStructCallbacks, "CallInitializeStruct");
        
        UObjectDefinition = UnrealSharpAssembly.FindType("UObject", CoreUObjectNamespace)!.Resolve();
        UActorComponentDefinition = UnrealSharpAssembly.FindType("UActorComponent", EngineNamespace)!.Resolve();
        
        TypeReference blittableType = UnrealSharpCoreAssembly.FindType(TypeDefinitionUtilities.BlittableTypeAttribute, UnrealSharpCoreAttributesNamespace)!;
        BlittableTypeConstructor = blittableType.FindMethod(".ctor")!;

        TypeReference generatedType = UnrealSharpCoreAssembly.FindType(GeneratedTypeAttribute, UnrealSharpCoreAttributesNamespace)!;
        GeneratedTypeCtor = generatedType.FindMethod(".ctor")!;
        
        ScriptInterfaceMarshaller = UnrealSharpAssembly.FindType("ScriptInterfaceMarshaller`1", CoreUObjectNamespace)!.Resolve();
        
        TypeReference typeExtensions = UnrealSharpAssembly.FindType("TypeExtensions", UnrealSharpNamespace)!;
        GetAssemblyNameMethod = typeExtensions.FindMethod("GetAssemblyName")!;
        
        TypeReference? typeType = UnrealSharpAssembly.MainModule.ImportReference(typeof(Type));
        GetTypeFromHandleMethod = typeType.FindMethod("GetTypeFromHandle")!;
    }

    private static MethodReference FindBindingsStaticMethod(string findNamespace, string findClass, string findMethod)
    {
        foreach (var module in UnrealSharpAssembly.Modules)
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

    private static MethodReference FindExporterMethod(string exporterName, string functionName)
    {
        return FindBindingsStaticMethod(InteropNameSpace, exporterName, functionName);
    }
}
