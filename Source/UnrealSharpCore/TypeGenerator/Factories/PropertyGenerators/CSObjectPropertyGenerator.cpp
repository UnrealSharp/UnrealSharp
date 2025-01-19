#include "CSObjectPropertyGenerator.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"
#include "TypeGenerator/Register/MetaData/CSDefaultComponentMetaData.h"
#include "TypeGenerator/Register/MetaData/CSObjectMetaData.h"

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

	REGISTER_METADATA(ECSPropertyType::Object, FCSObjectMetaData)
	REGISTER_METADATA(ECSPropertyType::WeakObject, FCSObjectMetaData)
	REGISTER_METADATA(ECSPropertyType::SoftObject, FCSObjectMetaData)
	REGISTER_METADATA(ECSPropertyType::ObjectPtr, FCSObjectMetaData)
	REGISTER_METADATA(ECSPropertyType::DefaultComponent, FCSDefaultComponentMetaData)

#if WITH_EDITOR
	AddPinType(ECSPropertyType::Object, UEdGraphSchema_K2::PC_Object);
	AddPinType(ECSPropertyType::WeakObject, UEdGraphSchema_K2::PC_Object);
	AddPinType(ECSPropertyType::SoftObject, UEdGraphSchema_K2::PC_SoftObject);
	AddPinType(ECSPropertyType::ObjectPtr, UEdGraphSchema_K2::PC_Object);
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

FEdGraphPinType UCSObjectPropertyGenerator::GetPinType(ECSPropertyType PropertyType, const FCSPropertyMetaData& MetaData, UBlueprint* Outer) const
{
	TSharedPtr<FCSObjectMetaData> ObjectMetaData = MetaData.GetTypeMetaData<FCSObjectMetaData>();
	UClass* Class = FCSTypeRegistry::GetClassFromName(ObjectMetaData->InnerType.Name);
	
	FEdGraphPinType PinType;
	PinType.PinCategory = TypeToPinType[PropertyType].PinCategory;
	PinType.PinSubCategoryObject = Class;
	PinType.bIsWeakPointer = PropertyType == ECSPropertyType::WeakObject;
	
	return PinType;
}

