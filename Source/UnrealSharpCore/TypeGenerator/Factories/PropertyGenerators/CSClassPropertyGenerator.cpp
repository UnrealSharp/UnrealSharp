#include "CSClassPropertyGenerator.h"
#include "MetaData/CSTemplateType.h"
#include "MetaData/FCSFieldTypePropertyMetaData.h"

FProperty* UCSClassPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FClassProperty* NewProperty = static_cast<FClassProperty*>(Super::CreateProperty(Outer, PropertyMetaData));

	TSharedPtr<FCSTemplateType> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSTemplateType>();
	const FCSPropertyMetaData* TemplateMetaData = ObjectMetaData->GetTemplateArgument(0);
	TSharedPtr<FCSFieldTypePropertyMetaData> InnerTypeMetaData = TemplateMetaData->GetTypeMetaData<FCSFieldTypePropertyMetaData>();
	UClass* Class = InnerTypeMetaData->InnerType.GetAsClass();
	
	NewProperty->PropertyClass = UClass::StaticClass();
	NewProperty->SetMetaClass(Class);
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSClassPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSTemplateType>();
}
