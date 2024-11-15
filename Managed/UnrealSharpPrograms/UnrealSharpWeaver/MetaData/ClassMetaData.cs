using Mono.Cecil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.TypeProcessors;

namespace UnrealSharpWeaver.MetaData;

public class ClassMetaData : TypeReferenceMetadata
{
    public TypeReferenceMetadata ParentClass { get; set; }
    public List<PropertyMetaData>? Properties { get; set; }
    public List<FunctionMetaData> Functions { get; set; }
    public List<FunctionMetaData> VirtualFunctions { get; set; }
    public List<string> Interfaces { get; set; }
    public string ConfigCategory { get; set; } 
    public ClassFlags ClassFlags { get; set; }
    
    // Non-serialized for JSON
    public bool HasProperties => Properties != null && Properties.Count > 0;
    private readonly TypeDefinition ClassDefinition;
    // End non-serialized
    
    public ClassMetaData(TypeDefinition type) : base(type, WeaverHelper.UClassAttribute)
    {
        ClassDefinition = type;
        
        PopulateInterfaces();
        PopulateProperties();
        PopulateFunctions();
        
        AddConfigCategory();
        
        ParentClass = new TypeReferenceMetadata(type.BaseType.Resolve());
        ClassFlags |= GetClassFlags(type, AttributeName) | ClassFlags.Native | ClassFlags.CompiledFromBlueprint;
        
        // Force DefaultConfig if Config is set and no other config flag is set
        if (ClassFlags.HasFlag(ClassFlags.Config) &&
            !ClassFlags.HasFlag(ClassFlags.GlobalUserConfig | ClassFlags.DefaultConfig | ClassFlags.ProjectUserConfig))
        {
            ClassFlags |= ClassFlags.DefaultConfig;
        }

        if (WeaverHelper.IsChildOf(type, WeaverHelper.UActorComponentDefinition))
        {
            TryAddMetaData("BlueprintSpawnableComponent", true);
        }
    }

    private void AddConfigCategory()
    {
        CustomAttribute? uClassAttribute = WeaverHelper.GetUClass(ClassDefinition);
        CustomAttributeArgument? configCategoryProperty = WeaverHelper.FindAttributeField(uClassAttribute, nameof(ConfigCategory));
        if (configCategoryProperty != null)
        {
            ConfigCategory = (string) configCategoryProperty.Value.Value;
        }
    }

    private void PopulateProperties()
    {
        if (ClassDefinition.Properties.Count == 0)
        {
            return;
        }
        
        Properties = [];
        
        foreach (PropertyDefinition property in ClassDefinition.Properties)
        {
            CustomAttribute? uPropertyAttribute = WeaverHelper.GetUProperty(property);

            if (uPropertyAttribute == null)
            {
                continue;
            }
            
            PropertyMetaData propertyMetaData = new PropertyMetaData(property);
            Properties.Add(propertyMetaData);
                
            if (propertyMetaData.IsInstancedReference)
            {
                ClassFlags |= ClassFlags.HasInstancedReference;
            }
        }
    }

    void PopulateFunctions()
    {
        if (ClassDefinition.Methods.Count == 0)
        {
            return;
        }
        
        Functions = [];
        VirtualFunctions = [];
        
        for (var i = ClassDefinition.Methods.Count - 1; i >= 0; i--)
        {
            MethodDefinition method = ClassDefinition.Methods[i];

            if (FunctionMetaData.IsAsyncUFunction(method))
            {
                FunctionProcessor.RewriteMethodAsAsyncUFunctionImplementation(method);
                continue;
            }
            
            bool isBlueprintOverride = FunctionMetaData.IsBlueprintEventOverride(method);
            bool isInterfaceFunction = FunctionMetaData.IsInterfaceFunction(method);
            
            if (WeaverHelper.IsUFunction(method) || (isInterfaceFunction && method.GetBaseMethod().DeclaringType == ClassDefinition))
            {
                if (isBlueprintOverride)
                {
                    throw new Exception($"{method.FullName} is a Blueprint override and cannot be marked as a UFunction again.");
                }
                
                FunctionMetaData functionMetaData = new FunctionMetaData(method);
                
                if (isInterfaceFunction && functionMetaData.FunctionFlags.HasFlag(EFunctionFlags.BlueprintNativeEvent))
                {
                    throw new Exception("Interface functions cannot be marked as BlueprintEvent. Mark base declaration as BlueprintEvent instead.");
                }
                
                Functions.Add(functionMetaData);
            }
            
            if (isBlueprintOverride || isInterfaceFunction)
            {
                VirtualFunctions.Add(new FunctionMetaData(method));
            }
        }
    }

    private static ClassFlags GetClassFlags(TypeReference classReference, string flagsAttributeName)
    {
        return (ClassFlags) GetFlags(classReference.Resolve().CustomAttributes, flagsAttributeName);
    }
    
    void PopulateInterfaces()
    {
        if (ClassDefinition.Interfaces.Count == 0)
        {
            return;
        }
        
        Interfaces = [];
        
        foreach (var typeInterface in ClassDefinition.Interfaces)
        {
            TypeDefinition interfaceType = typeInterface.InterfaceType.Resolve();

            if (interfaceType == WeaverHelper.IInterfaceType || !WeaverHelper.IsUInterface(interfaceType))
            {
                continue;
            }

            string interfaceNoPrefix = WeaverHelper.GetEngineName(interfaceType);
            Interfaces.Add(interfaceNoPrefix);
        }
    }
    
    public void PostWeaveCleanup()
    {
        foreach (FunctionMetaData function in Functions)
        {
            function.TryRemoveMethod();
        }
        
        foreach (FunctionMetaData virtualFunction in VirtualFunctions)
        {
            virtualFunction.TryRemoveMethod();
        }
    }
}