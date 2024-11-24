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

#if WITH_EDITOR
void UCSStructPropertyGenerator::CreatePinInfoEditor(const FCSPropertyMetaData& PropertyMetaData,
	FEdGraphPinType& PinType)
{
	PinType.PinCategory = UEdGraphSchema_K2::PC_Struct;
}

UObject* UCSStructPropertyGenerator::GetPinSubCategoryObject(UBlueprint* Blueprint, const FCSPropertyMetaData& PropertyMetaData) const
{
	TSharedPtr<FCSStructPropertyMetaData> StructPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSStructPropertyMetaData>();
	return FCSTypeRegistry::GetStructFromName(StructPropertyMetaData->TypeRef.Name);
}
#endif
