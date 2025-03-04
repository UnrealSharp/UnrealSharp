#include "CSMetaDataUtils.h"

#include "CSFieldName.h"
#include "CSUnrealSharpSettings.h"
#include "Dom/JsonObject.h"
#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "UObject/UnrealType.h"

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
	PropertiesMetaData.Type = FCSPropertyFactory::CreateTypeMetaData(PropertyMetaData);
	PropertiesMetaData.SerializeFromJson(PropertyMetaData);
}

FName FCSMetaDataUtils::GetAdjustedFieldName(const FCSFieldName& FieldName)
{
	FString Name;
	if (GetDefault<UCSUnrealSharpSettings>()->HasNamespaceSupport())
	{
		Name = FieldName.GetFullName().ToString();

		// Unreal doesn't really consider dots in names for editor display. So we replace them with underscores.
		Name.ReplaceInline(TEXT("."), TEXT("_"));
	}
	else
	{
		Name = FieldName.GetName();
	}

	return *Name;
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
