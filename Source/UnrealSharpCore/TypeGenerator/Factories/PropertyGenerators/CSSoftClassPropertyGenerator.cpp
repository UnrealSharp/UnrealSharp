#include "CSSoftClassPropertyGenerator.h"
#include "MetaData/CSTemplateType.h"
#include "MetaData/FCSFieldTypePropertyMetaData.h"

FProperty* UCSSoftClassPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FSoftClassProperty* NewProperty = static_cast<FSoftClassProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSTemplateType> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSTemplateType>();
	
	const FCSPropertyMetaData* TemplateMetaData = ObjectMetaData->GetTemplateArgument(0);
	TSharedPtr<FCSFieldTypePropertyMetaData> InnerTypeMetaData = TemplateMetaData->GetTypeMetaData<FCSFieldTypePropertyMetaData>();
	
	NewProperty->PropertyClass = UClass::StaticClass();
	NewProperty->SetMetaClass(InnerTypeMetaData->InnerType.GetAsClass());
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSSoftClassPropertyGenerator::CreateTypeMetaData(
	ECSPropertyType PropertyType)
{
	return MakeShared<FCSTemplateType>();
}
