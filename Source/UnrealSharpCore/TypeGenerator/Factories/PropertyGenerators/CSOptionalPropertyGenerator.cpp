#include "CSOptionalPropertyGenerator.h"
#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "MetaData/CSTemplateType.h"

struct FCSTemplateType;

FProperty* UCSOptionalPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FOptionalProperty* NewProperty = static_cast<FOptionalProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSTemplateType> OptionalPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSTemplateType>();
	NewProperty->SetValueProperty(FCSPropertyFactory::CreateProperty(Outer, *OptionalPropertyMetaData->GetTemplateArgument(0)));
	NewProperty->GetValueProperty()->Owner = NewProperty;
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSOptionalPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSTemplateType>();
}
