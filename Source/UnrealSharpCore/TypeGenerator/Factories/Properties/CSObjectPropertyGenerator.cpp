#include "CSObjectPropertyGenerator.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"
#include "TypeGenerator/Register/MetaData/CSObjectMetaData.h"

UCSObjectPropertyGenerator::UCSObjectPropertyGenerator(FObjectInitializer const& ObjectInitializer) : Super(ObjectInitializer)
{
	TypeToFieldClass =
	{
		{ ECSPropertyType::Object, FObjectProperty::StaticClass() },
		{ ECSPropertyType::WeakObject, FWeakObjectProperty::StaticClass() },
		{ ECSPropertyType::SoftObject, FSoftObjectProperty::StaticClass() },
		{ ECSPropertyType::ObjectPtr, FObjectProperty::StaticClass() },
	};

#if WITH_EDITOR
	PropertyTypeToPinCategory =
	{
		{ ECSPropertyType::Object, UEdGraphSchema_K2::PC_Object },
		{ ECSPropertyType::WeakObject, UEdGraphSchema_K2::PC_Object },
		{ ECSPropertyType::SoftObject, UEdGraphSchema_K2::PC_SoftObject },
		{ ECSPropertyType::ObjectPtr, UEdGraphSchema_K2::PC_Object },
	};
#endif
}

FProperty* UCSObjectPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FObjectProperty* Property = static_cast<FObjectProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	
	TSharedPtr<FCSObjectMetaData> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSObjectMetaData>();
	UClass* Class = FCSTypeRegistry::GetClassFromName(ObjectMetaData->InnerType.Name);

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

UObject* UCSObjectPropertyGenerator::GetPinSubCategoryObject(UBlueprint* Blueprint, const FCSPropertyMetaData& PropertyMetaData) const
{
	TSharedPtr<FCSObjectMetaData> ObjectMetaData = PropertyMetaData.GetTypeMetaData<FCSObjectMetaData>();
	return FCSTypeRegistry::GetClassFromName(ObjectMetaData->InnerType.Name);
}
