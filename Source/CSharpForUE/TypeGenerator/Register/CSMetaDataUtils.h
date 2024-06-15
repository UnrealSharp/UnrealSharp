#pragma once

#include "CoreMinimal.h"
#include "Misc/Guid.h"
#include "UObject/Script.h"
#include "Dom/JsonObject.h"
#include "MetaData/CSFunctionMetaData.h"
#include "UObject/ObjectMacros.h"

namespace FCSMetaDataUtils
{
	void SerializeFunctions(const TArray<TSharedPtr<FJsonValue>>& FunctionsInfo, TArray<FCSFunctionMetaData>& FunctionMetaData);
	void SerializeProperties(const TArray<TSharedPtr<FJsonValue>>& PropertiesInfo, TArray<FCSPropertyMetaData>& PropertiesMetaData, EPropertyFlags DefaultFlags = CPF_None);
	void SerializeProperty(const TSharedPtr<FJsonObject>& PropertyMetaData, FCSPropertyMetaData& PropertiesMetaData, EPropertyFlags DefaultFlags = CPF_None);

	template<typename FlagType>
	FlagType GetFlags(const TSharedPtr<FJsonObject>& PropertyInfo, const FString& StringField);
	
	void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject, TMap<FString, FString>& MetaDataMap);
	void ApplyMetaData(const TMap<FString, FString>& MetaDataMap, UField* Field);
	void ApplyMetaData(const TMap<FString, FString>& MetaDataMap, FField* Field);
}