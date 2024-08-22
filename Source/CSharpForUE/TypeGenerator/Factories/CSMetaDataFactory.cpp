#include "CSMetaDataFactory.h"
#include "Dom/JsonObject.h"
#include "TypeGenerator/Register/MetaData/CSArrayPropertyMetaData.h"
#include "TypeGenerator/Register/MetaData/CSClassPropertyMetaData.h"
#include "TypeGenerator/Register/MetaData/CSDefaultComponentMetaData.h"
#include "TypeGenerator/Register/MetaData/CSDelegateMetaData.h"
#include "TypeGenerator/Register/MetaData/CSEnumPropertyMetaData.h"
#include "TypeGenerator/Register/MetaData/CSMapPropertyMetaData.h"
#include "TypeGenerator/Register/MetaData/CSStructPropertyMetaData.h"

static TMap<ECSPropertyType, TFunction<TSharedPtr<FCSUnrealType>()>> MetaDataFactoryMap;

void CSMetaDataFactory::Initialize()
{
	if (!MetaDataFactoryMap.IsEmpty())
	{
		return;
	}
	
	REGISTER_METADATA(ECSPropertyType::Enum, FCSEnumPropertyMetaData)
	
	REGISTER_METADATA(ECSPropertyType::Delegate, FCSDelegateMetaData)
	REGISTER_METADATA(ECSPropertyType::MulticastInlineDelegate, FCSDelegateMetaData)
	REGISTER_METADATA(ECSPropertyType::MulticastSparseDelegate, FCSDelegateMetaData)

	REGISTER_METADATA(ECSPropertyType::Struct, FCSStructPropertyMetaData)
	
	REGISTER_METADATA(ECSPropertyType::Object, FCSObjectMetaData)
	REGISTER_METADATA(ECSPropertyType::WeakObject, FCSObjectMetaData)
	REGISTER_METADATA(ECSPropertyType::SoftObject, FCSObjectMetaData)
	
	REGISTER_METADATA(ECSPropertyType::SoftClass, FCSObjectMetaData)
	REGISTER_METADATA(ECSPropertyType::Class, FCSClassPropertyMetaData)
	
	REGISTER_METADATA(ECSPropertyType::Array, FCSArrayPropertyMetaData)
	REGISTER_METADATA(ECSPropertyType::DefaultComponent, FCSDefaultComponentMetaData)

	REGISTER_METADATA(ECSPropertyType::Map, FCSMapPropertyMetaData)
}

TSharedPtr<FCSUnrealType> CSMetaDataFactory::Create(const TSharedPtr<FJsonObject>& PropertyMetaData)
{
	Initialize();
	
	const TSharedPtr<FJsonObject>& PropertyTypeObject = PropertyMetaData->GetObjectField(TEXT("PropertyDataType"));
	ECSPropertyType PropertyType = static_cast<ECSPropertyType>(PropertyTypeObject->GetIntegerField(TEXT("PropertyType")));
	
	TSharedPtr<FCSUnrealType> MetaData;
	if (TFunction<TSharedPtr<FCSUnrealType>()>* FactoryMethod = MetaDataFactoryMap.Find(PropertyType))
	{
		MetaData = (*FactoryMethod)();
	}
	else
	{
		MetaData = MakeShared<FCSUnrealType>();
	}

	MetaData->SerializeFromJson(PropertyTypeObject);
	return MetaData;
}
