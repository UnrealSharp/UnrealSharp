#include "CSSetPropertyGenerator.h"

#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "MetaData/CSTemplateType.h"

FProperty* UCSSetPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FSetProperty* ArrayProperty = static_cast<FSetProperty*>(Super::CreateProperty(Outer, PropertyMetaData));

	TSharedPtr<FCSTemplateType> ArrayPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSTemplateType>();
	ArrayProperty->ElementProp = FCSPropertyFactory::CreateProperty(Outer, *ArrayPropertyMetaData->GetTemplateArgument(0));
	ArrayProperty->ElementProp->Owner = ArrayProperty;
	return ArrayProperty;
}

TSharedPtr<FCSUnrealType> UCSSetPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSTemplateType>();
}
