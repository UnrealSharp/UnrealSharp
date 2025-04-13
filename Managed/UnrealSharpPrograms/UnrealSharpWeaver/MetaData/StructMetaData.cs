﻿using Mono.Cecil;
using UnrealSharpWeaver.Utilities;
using PropertyUtilities = UnrealSharpWeaver.Utilities.PropertyUtilities;

namespace UnrealSharpWeaver.MetaData;

public class StructMetaData : TypeReferenceMetadata
{
    public List<PropertyMetaData> Fields { get; set; }
    public StructFlags StructFlags { get; set; }
    
    // Non-serialized for JSON
    public readonly bool IsBlittableStruct;
    // End non-serialized
    
    public StructMetaData(TypeDefinition structDefinition) : base(structDefinition, TypeDefinitionUtilities.UStructAttribute)
    {
        if (structDefinition.Properties.Count > 0)
        {
            throw new InvalidUnrealStructException(structDefinition, "UStructs don't support properties, use fields instead with UProperty attribute");
        }
        
        Fields = new List<PropertyMetaData>();
        IsBlittableStruct = true;
        
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
        
        CustomAttribute structFlagsAttribute = new CustomAttribute(WeaverImporter.BlittableTypeConstructor);
        structDefinition.CustomAttributes.Add(structFlagsAttribute);
        
        TryAddMetaData("BlueprintType", true);
    }
}