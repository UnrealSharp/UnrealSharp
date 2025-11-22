#include "Factories/PropertyGenerators/CSOptionalPropertyGenerator.h"
#include "Factories/CSPropertyFactory.h"
#include "ReflectionData/CSTemplateType.h"

struct FCSTemplateType;

FProperty* UCSOptionalPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
{
	FOptionalProperty* OptionalProperty = NewProperty<FOptionalProperty>(Outer, PropertyReflectionData);
	TSharedPtr<FCSTemplateType> TemplateType = PropertyReflectionData.GetInnerTypeData<FCSTemplateType>();
	OptionalProperty->SetValueProperty(FCSPropertyFactory::CreateProperty(Outer, *TemplateType->GetTemplateArgument(0)));
	OptionalProperty->GetValueProperty()->Owner = OptionalProperty;
	return OptionalProperty;
}

TSharedPtr<FCSUnrealType> UCSOptionalPropertyGenerator::CreatePropertyInnerTypeData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSTemplateType>();
}
