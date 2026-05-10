#pragma once

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
	
	UNREALSHARPUTILITIES_API TOptional<FValue::ConstMemberIterator> FindMember(FConstObject Object, FStringView FieldName);
	UNREALSHARPUTILITIES_API TOptional<TStringView<TCHAR>> GetStringField(FConstObject Object, FStringView FieldName);
	UNREALSHARPUTILITIES_API TOptional<bool> GetBoolField(FConstObject Object, const TCHAR* FieldName);
	UNREALSHARPUTILITIES_API TOptional<int32> GetInt32Field(FConstObject Object, const TCHAR* FieldName);
	UNREALSHARPUTILITIES_API TOptional<uint32> GetUint32Field(FConstObject Object, const TCHAR* FieldName);
	UNREALSHARPUTILITIES_API TOptional<int64> GetInt64Field(FConstObject Object, const TCHAR* FieldName);
	UNREALSHARPUTILITIES_API TOptional<uint64> GetUint64Field(FConstObject Object, const TCHAR* FieldName);
	UNREALSHARPUTILITIES_API TOptional<float> GetFloatField(FConstObject Object, const TCHAR* FieldName);
	UNREALSHARPUTILITIES_API TOptional<double> GetDoubleField(FConstObject Object, const TCHAR* FieldName);
	UNREALSHARPUTILITIES_API TOptional<FConstObject> GetObjectField(FConstObject Object, const TCHAR* FieldName);
	UNREALSHARPUTILITIES_API TOptional<FConstArray> GetArrayField(FConstObject Object, const TCHAR* FieldName);
};
