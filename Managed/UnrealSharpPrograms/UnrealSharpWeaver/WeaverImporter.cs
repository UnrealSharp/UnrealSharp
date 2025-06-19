using Mono.Cecil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver;

public class WeaverImporter
{
    private static WeaverImporter? _instance;
    public static WeaverImporter Instance => _instance ??= new WeaverImporter();

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
    
    public AssemblyDefinition UserAssembly = null!;
    public readonly ICollection<AssemblyDefinition> WeavedAssemblies = [];
    
    public AssemblyDefinition UnrealSharpAssembly => FindAssembly(UnrealSharpNamespace);
    public AssemblyDefinition UnrealSharpCoreAssembly => FindAssembly(UnrealSharpNamespace + ".Core");
    public AssemblyDefinition ProjectGlueAssembly => FindAssembly("ProjectGlue");
    
    public MethodReference NativeObjectGetter = null!;
    public TypeDefinition IntPtrType = null!;
    public MethodReference IntPtrAdd = null!;
    public FieldReference IntPtrZero = null!;
    public MethodReference IntPtrEqualsOperator = null!;
    public TypeReference UnrealSharpObjectType = null!;
    public TypeDefinition IInterfaceType = null!;
    public MethodReference GetNativeFunctionFromInstanceAndNameMethod = null!;
    public TypeReference Int32TypeRef = null!;
    public TypeReference VoidTypeRef = null!;
    public TypeReference ByteTypeRef = null!;
    public MethodReference GetNativeClassFromNameMethod = null!;
    public MethodReference GetNativeStructFromNameMethod = null!;
    public MethodReference GetPropertyOffsetFromNameMethod = null!;
    public MethodReference GetPropertyOffset = null!;
    public MethodReference GetNativePropertyFromNameMethod = null!;
    public MethodReference GetNativeFunctionFromClassAndNameMethod = null!;
    public MethodReference GetNativeFunctionParamsSizeMethod = null!;
    public MethodReference GetNativeStructSizeMethod = null!;
    public MethodReference GetSignatureFunction = null!;
    public MethodReference InitializeStructMethod = null!;
    
    public MethodReference InvokeNativeFunctionMethod = null!;
    public MethodReference InvokeNativeNetFunction = null!;
    public MethodReference InvokeNativeFunctionOutParms = null!;

    public MethodReference GeneratedTypeCtor = null!;
    
    public TypeDefinition UObjectDefinition = null!;
    public TypeDefinition UActorComponentDefinition = null!;
    
    public TypeDefinition ScriptInterfaceMarshaller = null!;
    
    public MethodReference BlittableTypeConstructor = null!;

    public DefaultAssemblyResolver AssemblyResolver = null!;
    
    public static void Shutdown()
    {
        _instance = null;
    }
    
    static AssemblyDefinition FindAssembly(string assemblyName)
    {
        return Instance.AssemblyResolver.Resolve(new AssemblyNameReference(assemblyName, new Version(0, 0)));
    }

    public void ImportCommonTypes(AssemblyDefinition userAssembly)
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
        InvokeNativeNetFunction = FindExporterMethod(UObjectCallbacks, "CallInvokeNativeNetFunction");
        InvokeNativeFunctionOutParms = FindExporterMethod(UObjectCallbacks, "CallInvokeNativeFunctionOutParms");
        
        GetSignatureFunction = FindExporterMethod(MulticastDelegatePropertyCallbacks, "CallGetSignatureFunction");
        
        InitializeStructMethod = FindExporterMethod(UStructCallbacks, "CallInitializeStruct");
        
        UObjectDefinition = UnrealSharpAssembly.FindType("UObject", CoreUObjectNamespace)!.Resolve();
        UActorComponentDefinition = UnrealSharpAssembly.FindType("UActorComponent", EngineNamespace)!.Resolve();
        
        TypeReference blittableType = UnrealSharpCoreAssembly.FindType(TypeDefinitionUtilities.BlittableTypeAttribute, UnrealSharpCoreAttributesNamespace)!;
        BlittableTypeConstructor = blittableType.FindMethod(".ctor")!;

        TypeReference generatedType = UnrealSharpCoreAssembly.FindType(GeneratedTypeAttribute, UnrealSharpCoreAttributesNamespace)!;
        GeneratedTypeCtor = generatedType.FindMethod(".ctor")!;
        
        ScriptInterfaceMarshaller = UnrealSharpAssembly.FindType("ScriptInterfaceMarshaller`1", CoreUObjectNamespace)!.Resolve();
    }

    private static MethodReference FindBindingsStaticMethod(string findNamespace, string findClass, string findMethod)
    {
        foreach (var module in Instance.UnrealSharpAssembly.Modules)
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
                        return Instance.UserAssembly.MainModule.ImportReference(method);
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
