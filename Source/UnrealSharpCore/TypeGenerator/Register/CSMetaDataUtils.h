#pragma once

#include "CoreMinimal.h"
#include "Dom/JsonObject.h"
#include "MetaData/CSFunctionMetaData.h"
#include "UObject/ObjectMacros.h"

struct FCSFieldName;

namespace FCSMetaDataUtils
{
	void SerializeFunctions(const TArray<TSharedPtr<FJsonValue>>& FunctionsInfo, TArray<FCSFunctionMetaData>& FunctionMetaData);
	void SerializeProperties(const TArray<TSharedPtr<FJsonValue>>& PropertiesInfo, TArray<FCSPropertyMetaData>& PropertiesMetaData, EPropertyFlags DefaultFlags = CPF_None);
	void SerializeProperty(const TSharedPtr<FJsonObject>& PropertyMetaData, FCSPropertyMetaData& PropertiesMetaData, EPropertyFlags DefaultFlags = CPF_None);

	template<typename FlagType>
	FlagType GetFlags(const TSharedPtr<FJsonObject>& PropertyInfo, const FString& StringField)
	{
		FString FoundStringField;
		PropertyInfo->TryGetStringField(StringField, FoundStringField);

		if (FoundStringField.IsEmpty())
		{
			return static_cast<FlagType>(0);
		}

		uint64 FunctionFlagsInt;
		TTypeFromString<uint64>::FromString(FunctionFlagsInt, *FoundStringField);
		return static_cast<FlagType>(FunctionFlagsInt);
	};
	
	void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject, TMap<FString, FString>& MetaDataMap);
	UNREALSHARPCORE_API void ApplyMetaData(const TMap<FString, FString>& MetaDataMap, UField* Field);
	UNREALSHARPCORE_API void ApplyMetaData(const TMap<FString, FString>& MetaDataMap, FField* Field);

	UNREALSHARPCORE_API FName GetAdjustedFieldName(const FCSFieldName& FieldName);
}
