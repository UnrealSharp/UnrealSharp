using Mono.Cecil;

using UnrealSharpWeaver.Rewriters;

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
    
    internal List<MethodDefinition> BlueprintEventOverrides;
    internal readonly TypeDefinition MyTypeDefinition;

    public ClassMetaData(TypeDefinition type) : base(type, "UClassAttribute")
    {
        MyTypeDefinition = type;
        
        PopulateInterfaces();
        PopulateProperties();
        PopulateFunctions();
        
        ParentClass = new TypeReferenceMetadata(type.BaseType.Resolve());
        ClassFlags = (ClassFlags) ExtractFlagsFromClass(type, "UClassAttribute");

        CustomAttribute? uClassAttribute = FindAttribute(type.CustomAttributes, "UClassAttribute");
        CustomAttributeNamedArgument configCategoryProperty = uClassAttribute.Properties.FirstOrDefault(prop => prop.Name == nameof(ConfigCategory));
        ConfigCategory = (string) configCategoryProperty.Argument.Value;
    }

    private void PopulateProperties()
    {
        Properties = [];
        
        foreach (PropertyDefinition property in MyTypeDefinition.Properties)
        {
            CustomAttribute? uPropertyAttribute = PropertyMetaData.GetUPropertyAttribute(property);
        
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
        BlueprintEventOverrides = [];
        
        for (int i = MyTypeDefinition.Methods.Count - 1; i >= 0; i--)
        {
            MethodDefinition method = MyTypeDefinition.Methods[i];

            if (FunctionMetaData.IsAsyncUFunction(method))
            {
                FunctionRewriterHelpers.RewriteMethodAsAsyncUFunctionImplementation(method);
            }
            else if (FunctionMetaData.IsUFunction(method))
            {
                Functions.Add(new FunctionMetaData(method));
            }
            else if (FunctionMetaData.IsBlueprintEventOverride(method) || FunctionMetaData.IsInterfaceFunction(MyTypeDefinition, method.Name))
            {
                BlueprintEventOverrides.Add(method);
                VirtualFunctions.Add(new FunctionMetaData(method));
            }
        }
    }
    
    void PopulateInterfaces()
    {
        if (MyTypeDefinition.Interfaces.Count == 0)
        {
            return;
        }
        
        Interfaces = new List<string>();
        
        foreach (var typeInterface in MyTypeDefinition.Interfaces)
        {
            var interfaceType = typeInterface.InterfaceType.Resolve();
            if (WeaverHelper.IsUnrealSharpInterface(interfaceType))
            {
                Interfaces.Add(interfaceType.Name);
            }
        }
    }
}