#include "Factories/PropertyGenerators/CSSetPropertyGenerator.h"
#include "Factories/CSPropertyFactory.h"
#include "ReflectionData/CSTemplateType.h"

FProperty* UCSSetPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
{
	FSetProperty* SetProperty = NewProperty<FSetProperty>(Outer, PropertyReflectionData);

	TSharedPtr<FCSTemplateType> ArrayPropertyMetaData = PropertyReflectionData.GetInnerTypeData<FCSTemplateType>();
	SetProperty->ElementProp = FCSPropertyFactory::CreateProperty(Outer, *ArrayPropertyMetaData->GetTemplateArgument(0));
	SetProperty->ElementProp->Owner = SetProperty;
	return SetProperty;
}

TSharedPtr<FCSUnrealType> UCSSetPropertyGenerator::CreatePropertyInnerTypeData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSTemplateType>();
}
