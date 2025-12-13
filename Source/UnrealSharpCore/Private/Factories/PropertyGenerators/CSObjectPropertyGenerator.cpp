#include "Factories/PropertyGenerators/CSObjectPropertyGenerator.h"
#include "ReflectionData/CSDefaultComponentType.h"
#include "ReflectionData/CSTemplateType.h"
#include "ReflectionData/CSFieldType.h"

UCSObjectPropertyGenerator::UCSObjectPropertyGenerator(FObjectInitializer const& ObjectInitializer) : Super(ObjectInitializer)
{
	TypeToFieldClass =
	{
		{ ECSPropertyType::Object, FObjectProperty::StaticClass() },
		{ ECSPropertyType::WeakObject, FWeakObjectProperty::StaticClass() },
		{ ECSPropertyType::SoftObject, FSoftObjectProperty::StaticClass() },
		{ ECSPropertyType::DefaultComponent , FObjectProperty::StaticClass() }
	};

	REGISTER_REFLECTION_DATA(ECSPropertyType::Object, FCSFieldType)
	REGISTER_REFLECTION_DATA(ECSPropertyType::WeakObject, FCSTemplateType)
	REGISTER_REFLECTION_DATA(ECSPropertyType::SoftObject, FCSTemplateType)
	REGISTER_REFLECTION_DATA(ECSPropertyType::DefaultComponent, FCSDefaultComponentType)
}

FProperty* UCSObjectPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
{
	FObjectProperty* ObjectProperty = NewProperty<FObjectProperty>(Outer, PropertyReflectionData, GetFieldClassForType(PropertyReflectionData));
	ECSPropertyType PropertyType = PropertyReflectionData.InnerType->PropertyType;

	UClass* Class;
	if (PropertyType == ECSPropertyType::WeakObject || PropertyType == ECSPropertyType::SoftObject)
	{
		TSharedPtr<FCSTemplateType> TemplateType = PropertyReflectionData.GetInnerTypeData<FCSTemplateType>();
		const FCSPropertyReflectionData* ArgumentReflectionData = TemplateType->GetTemplateArgument(0);
		TSharedPtr<FCSFieldType> FieldType = ArgumentReflectionData->GetInnerTypeData<FCSFieldType>();
		Class = FieldType->InnerType.GetAsClass();
	}
	else
	{
		TSharedPtr<FCSFieldType> FieldType = PropertyReflectionData.GetInnerTypeData<FCSFieldType>();
		Class = FieldType->InnerType.GetAsClass();
	}
	
	ObjectProperty->SetPropertyClass(Class);
	
	if (FLinkerLoad::IsImportLazyLoadEnabled())
	{
#if ENGINE_MINOR_VERSION >= 4
		ObjectProperty->SetPropertyFlags(CPF_TObjectPtrWrapper);
#else
		ObjectProperty->SetPropertyFlags(CPF_UObjectWrapper);
#endif
	}
	
	return ObjectProperty;
}

