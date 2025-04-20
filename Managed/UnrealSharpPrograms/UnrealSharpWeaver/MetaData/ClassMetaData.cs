using Mono.Cecil;
using Mono.Cecil.Rocks;
using UnrealSharpWeaver.TypeProcessors;
using UnrealSharpWeaver.Utilities;

namespace UnrealSharpWeaver.MetaData;

public class ClassMetaData : TypeReferenceMetadata
{
    public TypeReferenceMetadata ParentClass { get; set; }
    public List<PropertyMetaData> Properties { get; set; }
    public List<FunctionMetaData> Functions { get; set; }
    public List<FunctionMetaData> VirtualFunctions { get; set; }
    public List<TypeReferenceMetadata> Interfaces { get; set; }
    public string ConfigCategory { get; set; } 
    public ClassFlags ClassFlags { get; set; }
    
    // Non-serialized for JSON
    public bool HasProperties => Properties != null && Properties.Count > 0;
    private readonly TypeDefinition ClassDefinition;
    // End non-serialized
    
    public ClassMetaData(TypeDefinition type) : base(type, TypeDefinitionUtilities.UClassAttribute)
    {
        ClassDefinition = type;
        Properties = [];
        Functions = [];
        VirtualFunctions = [];
        
        ConfigCategory = string.Empty;
        Interfaces = [];
        
        PopulateInterfaces();
        PopulateProperties();
        PopulateFunctions();
        
        AddConfigCategory();
        
        ParentClass = new TypeReferenceMetadata(type.BaseType.Resolve());
        ClassFlags |= GetClassFlags(type, AttributeName) | ClassFlags.CompiledFromBlueprint;
        
        // Force DefaultConfig if Config is set and no other config flag is set
        if (ClassFlags.HasFlag(ClassFlags.Config) &&
            !ClassFlags.HasFlag(ClassFlags.GlobalUserConfig | ClassFlags.DefaultConfig | ClassFlags.ProjectUserConfig))
        {
            ClassFlags |= ClassFlags.DefaultConfig;
        }

        if (type.IsChildOf(WeaverImporter.Instance.UActorComponentDefinition))
        {
            TryAddMetaData("BlueprintSpawnableComponent", true);
        }
    }

    private void AddConfigCategory()
    {
        CustomAttribute uClassAttribute = ClassDefinition.GetUClass()!;
        CustomAttributeArgument? configCategoryProperty = uClassAttribute.FindAttributeField(nameof(ConfigCategory));
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
            CustomAttribute? uPropertyAttribute = property.GetUProperty();

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
            
            if (method.IsUFunction() && !isInterfaceFunction)
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
            
            if (isBlueprintOverride || (isInterfaceFunction && method.GetBaseMethod().DeclaringType == ClassDefinition))
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
        
        foreach (InterfaceImplementation? typeInterface in ClassDefinition.Interfaces)
        {
            TypeDefinition interfaceType = typeInterface.InterfaceType.Resolve();

            if (interfaceType == WeaverImporter.Instance.IInterfaceType || !interfaceType.IsUInterface())
            {
                continue;
            }
            
            Interfaces.Add(new TypeReferenceMetadata(interfaceType));
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