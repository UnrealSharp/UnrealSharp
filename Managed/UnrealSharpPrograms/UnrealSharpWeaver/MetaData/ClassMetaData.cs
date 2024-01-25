using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UnrealSharpWeaver.MetaData;

public class ClassMetaData : TypeReferenceMetadata
{
    public TypeReferenceMetadata ParentClass { get; set; }
    public List<PropertyMetaData> Properties { get; set; }
    public FunctionMetaData[] Functions { get; set; }
    public List<VirtualFunctionMetaData> VirtualFunctions { get; set; }
    public string[] Interfaces { get; set; }
    public string ConfigCategory { get; set; } 
    public ClassFlags ClassFlags { get; set; }
    
    internal MethodDefinition[] BlueprintEventOverrides;
    internal TypeDefinition MyTypeDefinition;

    public ClassMetaData(TypeDefinition type) : base(type, "UClassAttribute")
    {
        MyTypeDefinition = type;

        ClassFlags flags = (ClassFlags) ExtractClassAsFlags(type, "UClassAttribute");
        
        var invalidProperties = (from field in type.Fields where PropertyMetaData.IsUnrealProperty(field) select field).ToArray();

        SequencePoint point = ErrorEmitter.GetSequencePointFromMemberDefinition(MyTypeDefinition);
        string file = point.Document.Url;
        int line = point.StartLine;
        bool hasInvalidProperties = invalidProperties.Any();

        foreach (var prop in invalidProperties)
        {
            ErrorEmitter.Error("InvalidUnrealProperty", file, line, $"UProperties in a UClass must be property accessors. {prop.Name} is a field.");
        }
        
        Interfaces = type.Interfaces.Where(x => WeaverHelper.IsUnrealSharpInterface(x.InterfaceType.Resolve()))
            .Select(interfaceOnType => interfaceOnType.InterfaceType.Name)
            .ToArray();
        
        PopulateProperties(type);
        
        Functions = FunctionMetaData.PopulateFunctionArray(type);

        VirtualFunctions = type.Methods.Where(IsVirtualOrInterfaceMethod)
            .Select(x => new VirtualFunctionMetaData(x))
            .ToList();
        
        BlueprintEventOverrides = (from method in type.Methods 
            where FunctionMetaData.IsBlueprintEventOverride(method) 
            select method).ToArray();
        
        ParentClass = new TypeReferenceMetadata(type.BaseType.Resolve());
        ClassFlags = flags;

        var uClassAttribute = FindAttribute(type.CustomAttributes, "UClassAttribute");

        if (uClassAttribute != null)
        {
            var configCategoryProperty = uClassAttribute.Properties.FirstOrDefault(prop => prop.Name == nameof(ConfigCategory));
            ConfigCategory = (string) configCategoryProperty.Argument.Value;
        }

        if (hasInvalidProperties)
        {
            throw new InvalidUnrealClassException(MyTypeDefinition,"Errors in class declaration.");
        }
    }

    private void PopulateProperties(TypeDefinition type)
    {
        Properties = new List<PropertyMetaData>();
        
        foreach (PropertyDefinition property in type.Properties)
        {
            CustomAttribute? uPropertyAttribute = PropertyMetaData.GetUPropertyAttribute(property);
        
            if (uPropertyAttribute != null)
            {
                Properties.Add(new PropertyMetaData(property));
            }
        }
    }

    private bool IsVirtualOrInterfaceMethod(MethodDefinition method)
    {
        if (method.IsVirtual)
        {
            return true;
        }

        TypeDefinition? classToCheck = MyTypeDefinition;
        while (classToCheck != null)
        {
            if (classToCheck.Interfaces.Any(x => x.InterfaceType.Resolve().Methods.Any(y => y.Name == method.Name)))
            {
                return true;
            }

            classToCheck = classToCheck.BaseType?.Resolve();
        }

        return false;
    }
}