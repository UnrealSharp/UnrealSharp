#include "CSPropertyGenerator.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"
#if WITH_EDITOR
#include "Kismet2/BlueprintEditorUtils.h"
#endif

ECSPropertyType UCSPropertyGenerator::GetPropertyType() const
{
	return ECSPropertyType::Unknown;
}

FProperty* UCSPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	return NewProperty(Outer, PropertyMetaData);
}

bool UCSPropertyGenerator::SupportsPropertyType(ECSPropertyType InPropertyType) const
{
	ECSPropertyType PropertyType = GetPropertyType();
	check(PropertyType != ECSPropertyType::Unknown);
	return PropertyType == InPropertyType;
}

FGuid UCSPropertyGenerator::ConstructGUIDFromName(const FName& Name)
{
	return ConstructGUIDFromString(Name.ToString());
}

FGuid UCSPropertyGenerator::ConstructGUIDFromString(const FString& Name)
{
	const uint32 BufferLength = Name.Len() * sizeof(Name[0]);
	uint32 HashBuffer[5];
	FSHA1::HashBuffer(*Name, BufferLength, reinterpret_cast<uint8*>(HashBuffer));
	return FGuid(HashBuffer[1], HashBuffer[2], HashBuffer[3], HashBuffer[4]); 
}

bool UCSPropertyGenerator::CanBeHashed(const FProperty* InParam)
{
#if WITH_EDITOR
	if(InParam->IsA<FBoolProperty>())
	{
		return false;
	}

	if (InParam->IsA<FTextProperty>())
	{
		return false;
	}
	
	if (const FStructProperty* StructProperty = CastField<FStructProperty>(InParam))
	{
		return FBlueprintEditorUtils::StructHasGetTypeHash(StructProperty->Struct);
	}
#endif
	return true;
}

FProperty* UCSPropertyGenerator::NewProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData, const FFieldClass* FieldClass)
{
	FName PropertyName = PropertyMetaData.Name;
	
	if (EnumHasAnyFlags(PropertyMetaData.PropertyFlags, CPF_ReturnParm))
	{
		PropertyName = "ReturnValue";
	}

	if (FieldClass == nullptr)
	{
		FieldClass = GetPropertyClass();
	}
	
	FProperty* NewProperty = static_cast<FProperty*>(FieldClass->Construct(Outer, PropertyName, RF_Public));
	NewProperty->PropertyFlags = PropertyMetaData.PropertyFlags;
	return NewProperty;
}


