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

TSharedPtr<FCSUnrealType> UCSSoftClassPropertyGenerator::CreateTypeMetaData(
	ECSPropertyType PropertyType)
{
	return MakeShared<FCSObjectMetaData>();
}

#if WITH_EDITOR
FEdGraphPinType UCSSoftClassPropertyGenerator::GetPinType(ECSPropertyType PropertyType, const FCSPropertyMetaData& MetaData, UBlueprint* Outer) const
{
	TSharedPtr<FCSObjectMetaData> ObjectMetaData = MetaData.GetTypeMetaData<FCSObjectMetaData>();
	UClass* Class = FCSTypeRegistry::GetClassFromName(ObjectMetaData->InnerType.Name);
	
	FEdGraphPinType PinType;
	PinType.PinCategory = UEdGraphSchema_K2::PC_SoftClass;
	PinType.PinSubCategoryObject = Class;
	return PinType;
}
#endif
