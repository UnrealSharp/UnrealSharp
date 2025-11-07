#include "Factories/PropertyGenerators/CSClassPropertyGenerator.h"
#include "MetaData/CSTemplateType.h"
#include "MetaData/FCSFieldTypePropertyMetaData.h"

FProperty* UCSClassPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FClassProperty* ClassProperty = NewProperty<FClassProperty>(Outer, PropertyMetaData);

	TSharedPtr<FCSTemplateType> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSTemplateType>();
	const FCSPropertyMetaData* TemplateMetaData = ObjectMetaData->GetTemplateArgument(0);
	TSharedPtr<FCSFieldTypePropertyMetaData> InnerTypeMetaData = TemplateMetaData->GetTypeMetaData<FCSFieldTypePropertyMetaData>();
	UClass* Class = InnerTypeMetaData->InnerType.GetAsClass();
	
	ClassProperty->PropertyClass = UClass::StaticClass();
	ClassProperty->SetMetaClass(Class);
	return ClassProperty;
}

TSharedPtr<FCSUnrealType> UCSClassPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSTemplateType>();
}
