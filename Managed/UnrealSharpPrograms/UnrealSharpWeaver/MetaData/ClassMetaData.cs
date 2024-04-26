using Mono.Cecil;
using UnrealSharpWeaver.TypeProcessors;

namespace UnrealSharpWeaver.MetaData;

public class ClassMetaData : TypeReferenceMetadata
{
    public TypeReferenceMetadata ParentClass { get; set; }
    public List<PropertyMetaData> Properties { get; set; }
    public List<FunctionMetaData> Functions { get; set; }
    public List<FunctionMetaData> VirtualFunctions { get; set; }
    public List<string> Interfaces { get; set; }
    public string ConfigCategory { get; set; } 
    public ClassFlags ClassFlags { get; set; }

    private readonly TypeDefinition MyTypeDefinition;

    public ClassMetaData(TypeDefinition type) : base(type, WeaverHelper.UClassAttribute)
    {
        MyTypeDefinition = type;
        
        PopulateInterfaces();
        PopulateProperties();
        PopulateFunctions();
        AddConfigCategory();
        
        ParentClass = new TypeReferenceMetadata(type.BaseType.Resolve());
        ClassFlags = GetClassFlags(type, AttributeName);
    }

    private void AddConfigCategory()
    {
        CustomAttribute? uClassAttribute = WeaverHelper.GetUClass(MyTypeDefinition);
        CustomAttributeArgument? configCategoryProperty = WeaverHelper.FindAttributeField(uClassAttribute, nameof(ConfigCategory));
        if (configCategoryProperty != null)
        {
            ConfigCategory = (string) configCategoryProperty.Value.Value;
        }
    }

    private void PopulateProperties()
    {
        if (MyTypeDefinition.Properties.Count == 0)
        {
            return;
        }
        
        Properties = [];
        
        foreach (PropertyDefinition property in MyTypeDefinition.Properties)
        {
            CustomAttribute? uPropertyAttribute = WeaverHelper.GetUProperty(property);
        
            if (uPropertyAttribute != null)
            {
                Properties.Add(new PropertyMetaData(property));
            }
        }
    }

    void PopulateFunctions()
    {
        if (MyTypeDefinition.Methods.Count == 0)
        {
            return;
        }
        
        Functions = [];
        VirtualFunctions = [];
        
        for (var i = MyTypeDefinition.Methods.Count - 1; i >= 0; i--)
        {
            MethodDefinition method = MyTypeDefinition.Methods[i];

            if (FunctionMetaData.IsAsyncUFunction(method))
            {
                FunctionRewriterHelpers.RewriteMethodAsAsyncUFunctionImplementation(method);
                return;
            }

            if (method.Name.Contains("BeginPlay"))
            {
                Console.WriteLine("Found BeginPlay");
            }
            
            bool isBlueprintOverride = FunctionMetaData.IsBlueprintEventOverride(method);
            bool isInterfaceFunction = FunctionMetaData.IsInterfaceFunction(method);
            
            if (WeaverHelper.IsUFunction(method))
            {
                if (isBlueprintOverride)
                {
                    throw new Exception($"{method.FullName} is a Blueprint override and cannot be marked as a UFunction again.");
                }
                
                FunctionMetaData functionMetaData = new FunctionMetaData(method);
                
                if (isInterfaceFunction && functionMetaData.FunctionFlags.HasFlag(FunctionFlags.BlueprintNativeEvent))
                {
                    throw new Exception("Interface functions cannot be marked as BlueprintEvent. Mark base declaration as BlueprintEvent instead.");
                }
                
                Functions.Add(functionMetaData);
            }
            else if (isBlueprintOverride || isInterfaceFunction)
            {
                VirtualFunctions.Add(new FunctionMetaData(method, true));
            }
        }
    }

    private static ClassFlags GetClassFlags(TypeReference classReference, string flagsAttributeName)
    {
        return (ClassFlags) GetFlags(classReference.Resolve().CustomAttributes, flagsAttributeName);
    }
    
    void PopulateInterfaces()
    {
        if (MyTypeDefinition.Interfaces.Count == 0)
        {
            return;
        }
        
        Interfaces = [];
        
        foreach (var typeInterface in MyTypeDefinition.Interfaces)
        {
            var interfaceType = typeInterface.InterfaceType.Resolve();
            if (WeaverHelper.IsUInterface(interfaceType))
            {
                Interfaces.Add(interfaceType.Name);
            }
        }
    }
}