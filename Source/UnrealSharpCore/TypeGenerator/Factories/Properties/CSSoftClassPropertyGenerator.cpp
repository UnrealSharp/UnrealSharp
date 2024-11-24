#include "CSSoftClassPropertyGenerator.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"
#include "TypeGenerator/Register/MetaData/CSObjectMetaData.h"

struct FCSObjectMetaData;

FProperty* UCSSoftClassPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FSoftClassProperty* NewProperty = static_cast<FSoftClassProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSObjectMetaData> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSObjectMetaData>();
	UClass* Class = FCSTypeRegistry::GetClassFromName(ObjectMetaData->InnerType.Name);
	
	NewProperty->PropertyClass = UClass::StaticClass();
	NewProperty->SetMetaClass(Class);
	return NewProperty;
}

#if WITH_EDITOR
void UCSSoftClassPropertyGenerator::CreatePinInfoEditor(const FCSPropertyMetaData& PropertyMetaData, FEdGraphPinType& PinType)
{
	PinType.PinCategory = UEdGraphSchema_K2::PC_SoftClass;
}

UObject* UCSSoftClassPropertyGenerator::GetPinSubCategoryObject(UBlueprint* Blueprint, const FCSPropertyMetaData& PropertyMetaData) const
{
	TSharedPtr<FCSObjectMetaData> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSObjectMetaData>();
	return FCSTypeRegistry::GetClassFromName(ObjectMetaData->InnerType.Name);
}
#endif
