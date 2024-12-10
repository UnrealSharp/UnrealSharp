#include "CSSetPropertyGenerator.h"
#include "TypeGenerator/Register/MetaData/CSContainerBaseMetaData.h"

FProperty* UCSSetPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FSetProperty* ArrayProperty = static_cast<FSetProperty*>(Super::CreateProperty(Outer, PropertyMetaData));

	TSharedPtr<FCSContainerBaseMetaData> ArrayPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSContainerBaseMetaData>();
	ArrayProperty->ElementProp = CreateProperty(Outer, ArrayPropertyMetaData->InnerProperty);
	ArrayProperty->ElementProp->Owner = ArrayProperty;
	return ArrayProperty;
}

TSharedPtr<FCSUnrealType> UCSSetPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSContainerBaseMetaData>();
}
