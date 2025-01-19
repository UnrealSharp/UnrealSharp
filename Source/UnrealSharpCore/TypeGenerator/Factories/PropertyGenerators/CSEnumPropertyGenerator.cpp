#include "CSEnumPropertyGenerator.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"
#include "TypeGenerator/Register/MetaData/CSEnumPropertyMetaData.h"

FProperty* UCSEnumPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FEnumProperty* NewProperty = static_cast<FEnumProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	const TSharedPtr<FCSEnumPropertyMetaData> EnumPropertyMetaData = PropertyMetaData.GetTypeMetaData<
		FCSEnumPropertyMetaData>();
	
	UEnum* Enum = FCSTypeRegistry::GetEnumFromName(EnumPropertyMetaData->InnerProperty.Name);
	FByteProperty* UnderlyingProp = new FByteProperty(NewProperty, "UnderlyingType", RF_Public);
	
	NewProperty->SetEnum(Enum);
	NewProperty->AddCppProperty(UnderlyingProp);
	
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSEnumPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSEnumPropertyMetaData>();
}

#if WITH_EDITOR
FEdGraphPinType UCSEnumPropertyGenerator::GetPinType(ECSPropertyType PropertyType, const FCSPropertyMetaData& MetaData, UBlueprint* Outer) const
{
	TSharedPtr<FCSEnumPropertyMetaData> EnumMetaData = MetaData.GetTypeMetaData<FCSEnumPropertyMetaData>();
	UEnum* Enum = FCSTypeRegistry::GetEnumFromName(EnumMetaData->InnerProperty.Name);
	
	FEdGraphPinType PinType;
	PinType.PinCategory = UEdGraphSchema_K2::PC_Byte;
	PinType.PinSubCategoryObject = Enum;
	return PinType;
}
#endif
