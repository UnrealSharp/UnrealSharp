#include "CSMetaDataUtils.h"
#include "Dom/JsonObject.h"
#include "UObject/UnrealType.h"
#include "CSharpForUE/TypeGenerator/Factories/CSMetaDataFactory.h"

//START ----------------------CSharpMetaDataUtils----------------------------------------

void FCSMetaDataUtils::SerializeFunctions(const TArray<TSharedPtr<FJsonValue>>& FunctionsInfo, TArray<FCSFunctionMetaData>& FunctionMetaData)
{
	FunctionMetaData.Reserve(FunctionsInfo.Num());
	
	for (const TSharedPtr<FJsonValue>& FunctionInfo : FunctionsInfo)
	{
		FCSFunctionMetaData NewFunctionMetaData;
		NewFunctionMetaData.SerializeFromJson(FunctionInfo->AsObject());
		FunctionMetaData.Emplace(MoveTemp(NewFunctionMetaData));
	}
}

void FCSMetaDataUtils::SerializeProperties(const TArray<TSharedPtr<FJsonValue>>& PropertiesInfo, TArray<FCSPropertyMetaData>& PropertiesMetaData, EPropertyFlags DefaultFlags)
{
	PropertiesMetaData.Reserve(PropertiesInfo.Num());
	
	for (const TSharedPtr<FJsonValue>& Property : PropertiesInfo)
	{
		FCSPropertyMetaData NewPropertyMetaData;
		SerializeProperty(Property->AsObject(), NewPropertyMetaData, DefaultFlags);
		PropertiesMetaData.Emplace(MoveTemp(NewPropertyMetaData));
	}
}

void FCSMetaDataUtils::SerializeProperty(const TSharedPtr<FJsonObject>& PropertyMetaData, FCSPropertyMetaData& PropertiesMetaData, EPropertyFlags DefaultFlags)
{
	PropertiesMetaData.Type = CSMetaDataFactory::Create(PropertyMetaData);
	PropertiesMetaData.SerializeFromJson(PropertyMetaData);
}

void FCSMetaDataUtils::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject, TMap<FString, FString>& MetaDataMap)
{
	const TSharedPtr<FJsonObject>* MetaDataObjectPtr;
	if (JsonObject->TryGetObjectField(TEXT("MetaData"), MetaDataObjectPtr))
	{
		TSharedPtr<FJsonObject> MetaDataObject = *MetaDataObjectPtr;
		for (const auto& Pair : MetaDataObject->Values)
		{
			FString Key = Pair.Key;
			FString Value;
			
			MetaDataObject->TryGetStringField(Key, Value);

			MetaDataMap.Add(Key, Value);
		}
	}
}

void FCSMetaDataUtils::ApplyMetaData(const TMap<FString, FString>& MetaDataMap, UField* Field)
{
#if WITH_EDITOR
	for (const auto& MetaData : MetaDataMap)
	{
		Field->SetMetaData(*MetaData.Key, *MetaData.Value);
	}
#endif
}

void FCSMetaDataUtils::ApplyMetaData(const TMap<FString, FString>& MetaDataMap, FField* Field)
{
#if WITH_EDITOR
	for (const auto& MetaData : MetaDataMap)
	{
		Field->SetMetaData(*MetaData.Key, *MetaData.Value);
	}
#endif
}
