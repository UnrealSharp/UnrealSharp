#include "CSArrayPropertyGenerator.h"
#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "TypeGenerator/Register/MetaData/CSContainerBaseMetaData.h"

FProperty* UCSArrayPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FArrayProperty* NewProperty = static_cast<FArrayProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSContainerBaseMetaData> ArrayPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSContainerBaseMetaData>();
	NewProperty->Inner = FCSPropertyFactory::CreateProperty(Outer, ArrayPropertyMetaData->InnerProperty);
	NewProperty->Inner->Owner = NewProperty;
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSArrayPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSContainerBaseMetaData>();
}

#if WITH_EDITOR
FEdGraphPinType UCSArrayPropertyGenerator::GetPinType(ECSPropertyType PropertyType, const FCSPropertyMetaData& MetaData, UBlueprint* Outer) const
{
	TSharedPtr<FCSContainerBaseMetaData> ArrayPropertyMetaData = MetaData.GetTypeMetaData<FCSContainerBaseMetaData>();
	ECSPropertyType InnerPropertyType = ArrayPropertyMetaData->InnerProperty.Type->PropertyType;
	UCSPropertyGenerator* PropertyGenerator = FCSPropertyFactory::FindPropertyGenerator(InnerPropertyType);
	
	FEdGraphPinType PinType = PropertyGenerator->GetPinType(InnerPropertyType, ArrayPropertyMetaData->InnerProperty, Outer);
	PinType.ContainerType = EPinContainerType::Array;
	
	return PinType;
}
#endif
