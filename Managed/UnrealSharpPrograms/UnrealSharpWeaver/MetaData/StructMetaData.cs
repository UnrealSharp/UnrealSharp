using Mono.Cecil;

namespace UnrealSharpWeaver.MetaData;

public class StructMetaData : BaseMetaData
{
    public List<PropertyMetaData> Fields { get; set; }
    public StructFlags StructFlags { get; set; }
    
    // Non-serialized for JSON
    public readonly bool IsBlittableStruct;
    // End non-serialized
    
    public StructMetaData(TypeDefinition structDefinition) : base(structDefinition, WeaverHelper.UStructAttribute)
    {
        if (structDefinition.Properties.Count > 0)
        {
            throw new InvalidUnrealStructException(structDefinition, "UProperties in a UStruct must be fields, not property accessors.");
        }
        
        Fields = new List<PropertyMetaData>();
        foreach (var field in structDefinition.Fields)
        {
            if (field.IsStatic || !WeaverHelper.IsUProperty(field))
            {
                continue;
            }
            
            PropertyMetaData property = new PropertyMetaData(field);
            Fields.Add(property);

            if (property.IsInstancedReference)
            {
                StructFlags |= StructFlags.HasInstancedReference;
            }
        }
        
        IsBlittableStruct = true;
        bool isPlainOldData = true;
        
        foreach (var prop in Fields)
        {
            if (!prop.PropertyDataType.IsBlittable)
            {
                IsBlittableStruct = false;
            }
            
            if (!prop.PropertyDataType.IsPlainOldData)
            {
                isPlainOldData = false;
            }
        }
        
        StructFlags |= (StructFlags) GetFlags(structDefinition, "StructFlagsMapAttribute");

        if (isPlainOldData)
        {
            StructFlags |= StructFlags.IsPlainOldData;
            StructFlags |= StructFlags.NoDestructor;
            StructFlags |= StructFlags.ZeroConstructor;
        }

        if (IsBlittableStruct)
        {
            CustomAttribute structFlagsAttribute = new CustomAttribute(WeaverHelper.BlittableTypeConstructor);
            structDefinition.CustomAttributes.Add(structFlagsAttribute);
        }
    }
}