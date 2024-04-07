#include "CSMetaDataFactory.h"
#include "Dom/JsonObject.h"
#include "CSharpForUE/TypeGenerator/Register/CSMetaData.h"

static TMap<ECSPropertyType, TFunction<TSharedPtr<FUnrealType>()>> MetaDataFactoryMap;

void CSMetaDataFactory::Initialize()
{
	if (!MetaDataFactoryMap.IsEmpty())
	{
		return;
	}
	
	REGISTER_METADATA(ECSPropertyType::Enum, FEnumPropertyMetaData)
	
	REGISTER_METADATA(ECSPropertyType::Delegate, FDelegateMetaData)
	REGISTER_METADATA(ECSPropertyType::MulticastInlineDelegate, FDelegateMetaData)
	REGISTER_METADATA(ECSPropertyType::MulticastSparseDelegate, FDelegateMetaData)

	REGISTER_METADATA(ECSPropertyType::Struct, FStructPropertyMetaData)
	
	REGISTER_METADATA(ECSPropertyType::Object, FObjectMetaData)
	REGISTER_METADATA(ECSPropertyType::WeakObject, FObjectMetaData)
	REGISTER_METADATA(ECSPropertyType::SoftObject, FObjectMetaData)
	
	REGISTER_METADATA(ECSPropertyType::SoftClass, FObjectMetaData)
	REGISTER_METADATA(ECSPropertyType::Class, FClassPropertyMetaData)
	
	REGISTER_METADATA(ECSPropertyType::Array, FArrayPropertyMetaData)
	REGISTER_METADATA(ECSPropertyType::DefaultComponent, FDefaultComponentMetaData)
}

TSharedPtr<FUnrealType> CSMetaDataFactory::Create(const TSharedPtr<FJsonObject>& PropertyMetaData)
{
	Initialize();
	
	const TSharedPtr<FJsonObject>& PropertyTypeObject = PropertyMetaData->GetObjectField("PropertyDataType");
	ECSPropertyType PropertyType = static_cast<ECSPropertyType>(PropertyTypeObject->GetIntegerField("PropertyType"));
	
	TSharedPtr<FUnrealType> MetaData;
	if (TFunction<TSharedPtr<FUnrealType>()>* FactoryMethod = MetaDataFactoryMap.Find(PropertyType))
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
