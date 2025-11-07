#include "CSOptionalPropertyGenerator.h"
#include "Factories/CSPropertyFactory.h"
#include "MetaData/CSTemplateType.h"

struct FCSTemplateType;

FProperty* UCSOptionalPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FOptionalProperty* OptionalProperty = NewProperty<FOptionalProperty>(Outer, PropertyMetaData);
	TSharedPtr<FCSTemplateType> OptionalPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSTemplateType>();
	OptionalProperty->SetValueProperty(FCSPropertyFactory::CreateProperty(Outer, *OptionalPropertyMetaData->GetTemplateArgument(0)));
	OptionalProperty->GetValueProperty()->Owner = OptionalProperty;
	return OptionalProperty;
}

TSharedPtr<FCSUnrealType> UCSOptionalPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSTemplateType>();
}
