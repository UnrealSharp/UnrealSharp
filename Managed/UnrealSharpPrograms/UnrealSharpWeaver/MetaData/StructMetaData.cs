using System.Text.RegularExpressions;
using Mono.Cecil;
using UnrealSharpWeaver.Utilities;
using PropertyUtilities = UnrealSharpWeaver.Utilities.PropertyUtilities;

namespace UnrealSharpWeaver.MetaData;

public partial class StructMetaData : TypeReferenceMetadata
{
    public List<PropertyMetaData> Fields { get; set; }
    public StructFlags StructFlags { get; set; }
    
    // Non-serialized for JSON
    public readonly bool IsBlittableStruct;
    // End non-serialized
    
    public StructMetaData(TypeDefinition structDefinition) : base(structDefinition, TypeDefinitionUtilities.UStructAttribute)
    {
        Fields = new List<PropertyMetaData>();
        IsBlittableStruct = true;
        

        var backingFieldRegex = BackingFieldRegex();
        foreach (var field in structDefinition.Fields)
        {
            if (field.IsStatic)
            {
                continue;
            }
            
            if (!field.IsUProperty())
            {
                // Struct is not blittable if it has non-UProperty fields
                IsBlittableStruct = false;
            }
            
            PropertyMetaData property = new PropertyMetaData(field);
            
            // If we match against a backing property field use the property name instead.
            var backingFieldMatch = backingFieldRegex.Match(field.Name);
            if (backingFieldMatch.Success)
            {
                string propertyName = backingFieldMatch.Groups[1].Value;
                property.Name = propertyName;
                
            }

            if (property.IsInstancedReference)
            {
                StructFlags |= StructFlags.HasInstancedReference;
            }
            
            Fields.Add(property);
        }
        
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

        if (!IsBlittableStruct)
        {
            return;
        }
        
        CustomAttribute structFlagsAttribute = new CustomAttribute(WeaverImporter.Instance.BlittableTypeConstructor);
        structDefinition.CustomAttributes.Add(structFlagsAttribute);
        
        TryAddMetaData("BlueprintType", true);
    }

    [GeneratedRegex("<([a-zA-Z$_][a-zA-Z0-9$_]*)>k__BackingField")]
    private static partial Regex BackingFieldRegex();
}