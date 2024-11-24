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

#if WITH_EDITOR
void UCSArrayPropertyGenerator::CreatePinInfoEditor(const FCSPropertyMetaData& PropertyMetaData,
	FEdGraphPinType& PinType)
{
	PinType.ContainerType = EPinContainerType::Array;
}
#endif
