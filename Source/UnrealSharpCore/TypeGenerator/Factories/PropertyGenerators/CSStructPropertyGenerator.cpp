#include "CSStructPropertyGenerator.h"

#include "TypeGenerator/Register/CSTypeRegistry.h"
#include "TypeGenerator/Register/MetaData/CSStructPropertyMetaData.h"

FProperty* UCSStructPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FStructProperty* StructProperty = static_cast<FStructProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSStructPropertyMetaData> StructPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSStructPropertyMetaData>();
	StructProperty->Struct = FCSTypeRegistry::GetStructFromName(StructPropertyMetaData->TypeRef.Name);
	ensureAlways(StructProperty->Struct);
	return StructProperty;
}

TSharedPtr<FCSUnrealType> UCSStructPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSStructPropertyMetaData>();
}

FEdGraphPinType UCSStructPropertyGenerator::GetPinType(ECSPropertyType PropertyType, const FCSPropertyMetaData& MetaData, UBlueprint* Outer) const
{
	FEdGraphPinType PinType;
	PinType.PinCategory = UEdGraphSchema_K2::PC_Struct;
	TSharedPtr<FCSStructPropertyMetaData> StructPropertyMetaData = MetaData.GetTypeMetaData<FCSStructPropertyMetaData>();
	UScriptStruct* Struct = FCSTypeRegistry::GetStructFromName(StructPropertyMetaData->TypeRef.Name);
	PinType.PinSubCategoryObject = Struct;
	return PinType;
}
