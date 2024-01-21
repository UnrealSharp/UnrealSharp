#include "CSMetaDataFactory.h"
#include "Dom/JsonObject.h"
#include "CSharpForUE/TypeGenerator/Register/CSMetaData.h"

static TMap<FName, TFunction<TSharedPtr<FUnrealType>()>> MetaDataFactoryMap;

void CSMetaDataFactory::Initialize()
{
	if (!MetaDataFactoryMap.IsEmpty())
	{
		return;
	}
	
	REGISTER_METADATA(FEnumProperty, FEnumPropertyMetaData)
	REGISTER_METADATA(FMulticastInlineDelegateProperty, FMulticastDelegateMetaData)
	REGISTER_METADATA(FStructProperty, FStructPropertyMetaData)
	REGISTER_METADATA(FObjectProperty, FObjectMetaData)
	REGISTER_METADATA(FWeakObjectProperty, FObjectMetaData)
	REGISTER_METADATA(FSoftObjectProperty, FObjectMetaData)
	REGISTER_METADATA(FSoftClassProperty, FObjectMetaData)
	REGISTER_METADATA(FClassProperty, FClassPropertyMetaData)
	REGISTER_METADATA(FArrayProperty, FArrayPropertyMetaData)
	REGISTER_METADATA_WITH_NAME("DefaultComponent", FDefaultComponentMetaData)
}

TSharedPtr<FUnrealType> CSMetaDataFactory::Create(const TSharedPtr<FJsonObject>& PropertyMetaData)
{
	Initialize();
	
	TSharedPtr<FJsonObject> PropertyTypeObject = PropertyMetaData->GetObjectField("PropertyDataType");
	const FName PropertyClass = *PropertyTypeObject->GetStringField("UnrealPropertyClass");
	
	TSharedPtr<FUnrealType> MetaData;
	if (TFunction<TSharedPtr<FUnrealType>()>* FactoryMethod = MetaDataFactoryMap.Find(PropertyClass))
	{
		MetaData = (*FactoryMethod)();
	}
	else
	{
		MetaData = MakeShared<FUnrealType>();
	}

	MetaData->SerializeFromJson(PropertyTypeObject);
	return MetaData;
}
