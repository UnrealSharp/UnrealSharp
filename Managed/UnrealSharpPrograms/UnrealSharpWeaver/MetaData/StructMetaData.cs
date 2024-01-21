using Mono.Cecil;

namespace UnrealSharpWeaver.MetaData;

public class StructMetaData : BaseMetaData
{
    public PropertyMetaData[] Fields { get; set; }
    public StructFlags StructFlags { get; set; }
    public bool BlittableStruct { get; set; }
    public bool IsDataTableStruct { get; set; }
    
    public StructMetaData(TypeDefinition structDefinition)
    {
        Name = structDefinition.Name;
        Namespace = structDefinition.Namespace;
        AssemblyName = structDefinition.Module.Assembly.Name.Name;

        if (structDefinition.Properties.Count > 0)
        {
            throw new InvalidUnrealStructException(structDefinition, "UProperties in a UStruct must be fields, not property accessors.");
        }
        
        Fields = new PropertyMetaData[structDefinition.Fields.Count];
        for (var i = 0; i < Fields.Length; i++)
        {
            Fields[i] = new PropertyMetaData(structDefinition.Fields[i]);
        }
        
        AddMetadataAttributes(structDefinition.CustomAttributes);
        CheckIfDataTableStruct(structDefinition);
        
        BlittableStruct = true;
        bool isPlainOldData = true;
        
        foreach (var prop in Fields)
        {
            if (!prop.PropertyDataType.IsBlittable)
            {
                BlittableStruct = false;
            }
            
            if (!prop.PropertyDataType.IsPlainOldData)
            {
                isPlainOldData = false;
            }
        }
        
        StructFlags = (StructFlags) GetFlags(structDefinition, "StructFlagsMapAttribute");

        if (isPlainOldData)
        {
            StructFlags |= StructFlags.IsPlainOldData;
            StructFlags |= StructFlags.NoDestructor;
            StructFlags |= StructFlags.ZeroConstructor;
        }

        if (BlittableStruct)
        {
            CustomAttribute? structAttribute = FindAttribute(structDefinition.CustomAttributes, "UStructAttribute");

            if (structAttribute == null)
            {
                return;
            }

            structAttribute.Fields.Add(new CustomAttributeNamedArgument("BlittableStruct", new CustomAttributeArgument(structDefinition.Module.TypeSystem.Boolean, true)));
        }
    }

    void CheckIfDataTableStruct(TypeDefinition structDefinition)
    {
        foreach (var structInterface in structDefinition.Interfaces)
        {
            if (structInterface.InterfaceType.Name == "ITableRowBase")
            {
                IsDataTableStruct = true;
            }
        }
    }
}