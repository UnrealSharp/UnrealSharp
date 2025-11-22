#include "Factories/PropertyGenerators/CSClassPropertyGenerator.h"
#include "ReflectionData/CSTemplateType.h"
#include "ReflectionData/CSFieldType.h"

FProperty* UCSClassPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
{
	FClassProperty* ClassProperty = NewProperty<FClassProperty>(Outer, PropertyReflectionData);

	TSharedPtr<FCSTemplateType> ClassTemplateType = PropertyReflectionData.GetInnerTypeData<FCSTemplateType>();
	const FCSPropertyReflectionData* TemplateArgument = ClassTemplateType->GetTemplateArgument(0);
	
	TSharedPtr<FCSFieldType> FieldType = TemplateArgument->GetInnerTypeData<FCSFieldType>();
	UClass* Class = FieldType->InnerType.GetAsClass();
	
	ClassProperty->PropertyClass = UClass::StaticClass();
	ClassProperty->SetMetaClass(Class);
	
	return ClassProperty;
}

TSharedPtr<FCSUnrealType> UCSClassPropertyGenerator::CreatePropertyInnerTypeData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSTemplateType>();
}
