#include "CSSoftClassPropertyGenerator.h"
#include "MetaData/CSTemplateType.h"
#include "MetaData/FCSFieldTypePropertyMetaData.h"

FProperty* UCSSoftClassPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FSoftClassProperty* SoftClassProperty = NewProperty<FSoftClassProperty>(Outer, PropertyMetaData);
	TSharedPtr<FCSTemplateType> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSTemplateType>();
	
	const FCSPropertyMetaData* TemplateMetaData = ObjectMetaData->GetTemplateArgument(0);
	TSharedPtr<FCSFieldTypePropertyMetaData> InnerTypeMetaData = TemplateMetaData->GetTypeMetaData<FCSFieldTypePropertyMetaData>();
	
	SoftClassProperty->PropertyClass = UClass::StaticClass();
	SoftClassProperty->SetMetaClass(InnerTypeMetaData->InnerType.GetAsClass());
	return SoftClassProperty;
}

TSharedPtr<FCSUnrealType> UCSSoftClassPropertyGenerator::CreateTypeMetaData(
	ECSPropertyType PropertyType)
{
	return MakeShared<FCSTemplateType>();
}
