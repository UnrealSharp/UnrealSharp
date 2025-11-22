#include "Factories/PropertyGenerators/CSSoftClassPropertyGenerator.h"
#include "ReflectionData/CSTemplateType.h"
#include "ReflectionData/CSFieldType.h"

FProperty* UCSSoftClassPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
{
	FSoftClassProperty* SoftClassProperty = NewProperty<FSoftClassProperty>(Outer, PropertyReflectionData);
	TSharedPtr<FCSTemplateType> TemplateType = PropertyReflectionData.GetInnerTypeData<FCSTemplateType>();
	
	const FCSPropertyReflectionData* TemplateMetaData = TemplateType->GetTemplateArgument(0);
	TSharedPtr<FCSFieldType> FieldType = TemplateMetaData->GetInnerTypeData<FCSFieldType>();
	
	SoftClassProperty->PropertyClass = UClass::StaticClass();
	SoftClassProperty->SetMetaClass(FieldType->InnerType.GetAsClass());
	return SoftClassProperty;
}

TSharedPtr<FCSUnrealType> UCSSoftClassPropertyGenerator::CreatePropertyInnerTypeData(
	ECSPropertyType PropertyType)
{
	return MakeShared<FCSTemplateType>();
}
