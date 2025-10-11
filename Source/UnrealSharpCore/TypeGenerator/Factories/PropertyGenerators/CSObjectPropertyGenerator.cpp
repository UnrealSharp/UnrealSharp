#include "CSObjectPropertyGenerator.h"
#include "MetaData/CSDefaultComponentMetaData.h"
#include "MetaData/CSTemplateType.h"
#include "MetaData/FCSFieldTypePropertyMetaData.h"

UCSObjectPropertyGenerator::UCSObjectPropertyGenerator(FObjectInitializer const& ObjectInitializer) : Super(ObjectInitializer)
{
	TypeToFieldClass =
	{
		{ ECSPropertyType::Object, FObjectProperty::StaticClass() },
		{ ECSPropertyType::WeakObject, FWeakObjectProperty::StaticClass() },
		{ ECSPropertyType::SoftObject, FSoftObjectProperty::StaticClass() },
		{ ECSPropertyType::ObjectPtr, FObjectProperty::StaticClass() },
		{ ECSPropertyType::DefaultComponent , FObjectProperty::StaticClass() }
	};

	REGISTER_METADATA(ECSPropertyType::Object, FCSFieldTypePropertyMetaData)
	REGISTER_METADATA(ECSPropertyType::WeakObject, FCSTemplateType)
	REGISTER_METADATA(ECSPropertyType::SoftObject, FCSTemplateType)
	REGISTER_METADATA(ECSPropertyType::DefaultComponent, FCSDefaultComponentMetaData)
}

FProperty* UCSObjectPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FObjectProperty* Property = static_cast<FObjectProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	ECSPropertyType PropertyType = PropertyMetaData.Type->PropertyType;

	UClass* Class;
	if (PropertyType == ECSPropertyType::WeakObject || PropertyType == ECSPropertyType::SoftObject)
	{
		TSharedPtr<FCSTemplateType> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSTemplateType>();
		const FCSPropertyMetaData* TemplateMetaData = ObjectMetaData->GetTemplateArgument(0);
		TSharedPtr<FCSFieldTypePropertyMetaData> InnerTypeMetaData = TemplateMetaData->GetTypeMetaData<FCSFieldTypePropertyMetaData>();
		Class = InnerTypeMetaData->InnerType.GetAsClass();
	}
	else
	{
		TSharedPtr<FCSFieldTypePropertyMetaData> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSFieldTypePropertyMetaData>();
		Class = ObjectMetaData->InnerType.GetAsClass();
	}
	
	Property->SetPropertyClass(Class);
	
	if (FLinkerLoad::IsImportLazyLoadEnabled())
	{
#if ENGINE_MINOR_VERSION >= 4
		Property->SetPropertyFlags(CPF_TObjectPtrWrapper);
#else
		Property->SetPropertyFlags(CPF_UObjectWrapper);
#endif
	}
	
	return Property;
}

