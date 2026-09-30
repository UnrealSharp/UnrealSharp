#pragma once

#include "CoreMinimal.h"

#include <rapidjson/document.h>
#include <rapidjson/encodings.h>

namespace UnrealSharp::RapidJson
{
	using FEncoding = rapidjson::UTF16<TCHAR>;
	using FDocument = rapidjson::GenericDocument<FEncoding>;
	using FValue = FDocument::ValueType;
	using FConstObject = FValue::ConstObject;
	using FConstArray = FValue::ConstArray;

	UNREALSHARPUTILITIES_API bool ParseJsonString(TCHAR* JsonText, FDocument& OutDocument);
	UNREALSHARPUTILITIES_API TOptional<FConstObject> GetRootObject(const FDocument& Document);

	UNREALSHARPUTILITIES_API TOptional<FValue::ConstMemberIterator> FindMember(const FConstObject& Object, FStringView FieldName);
	UNREALSHARPUTILITIES_API TOptional<FStringView> GetStringField(const FConstObject& Object, FStringView FieldName);
	UNREALSHARPUTILITIES_API TOptional<bool> GetBoolField(const FConstObject& Object, FStringView FieldName);
	UNREALSHARPUTILITIES_API TOptional<int32> GetInt32Field(const FConstObject& Object, FStringView FieldName);
	UNREALSHARPUTILITIES_API TOptional<uint32> GetUint32Field(const FConstObject& Object, FStringView FieldName);
	UNREALSHARPUTILITIES_API TOptional<int64> GetInt64Field(const FConstObject& Object, FStringView FieldName);
	UNREALSHARPUTILITIES_API TOptional<uint64> GetUint64Field(const FConstObject& Object, FStringView FieldName);
	UNREALSHARPUTILITIES_API TOptional<float> GetFloatField(const FConstObject& Object, FStringView FieldName);
	UNREALSHARPUTILITIES_API TOptional<double> GetDoubleField(const FConstObject& Object, FStringView FieldName);
	UNREALSHARPUTILITIES_API TOptional<FConstObject> GetObjectField(const FConstObject& Object, FStringView FieldName);
	UNREALSHARPUTILITIES_API TOptional<FConstArray> GetArrayField(const FConstObject& Object, FStringView FieldName);
}
