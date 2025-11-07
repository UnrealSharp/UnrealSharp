#include "CSSetPropertyGenerator.h"

#include "Factories/CSPropertyFactory.h"
#include "MetaData/CSTemplateType.h"

FProperty* UCSSetPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FSetProperty* SetProperty = NewProperty<FSetProperty>(Outer, PropertyMetaData);

	TSharedPtr<FCSTemplateType> ArrayPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSTemplateType>();
	SetProperty->ElementProp = FCSPropertyFactory::CreateProperty(Outer, *ArrayPropertyMetaData->GetTemplateArgument(0));
	SetProperty->ElementProp->Owner = SetProperty;
	return SetProperty;
}

TSharedPtr<FCSUnrealType> UCSSetPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSTemplateType>();
}
