#include "Json/CSRapidJsonUtilties.h"
#include "Json/CSJsonUtilities.h"

bool UnrealSharp::RapidJson::ParseJsonString(TCHAR* JsonText, FDocument& OutDocument)
{
	rapidjson::GenericInsituStringStream<rapidjson::UTF16LE<TCHAR>> Stream(JsonText);

	FDocument Result;
	Result.ParseStream<rapidjson::kParseInsituFlag>(Stream);
	
	if (Result.HasParseError())
	{
		UE_LOGFMT(LogCSJsonUtilties, Error, "Failed to parse JSON string. Error code: {0}", Result.GetParseError());
		return false;
	}
	
	OutDocument = MoveTemp(Result);
	return true;
}

TOptional<FConstObject> UnrealSharp::RapidJson::GetRootObject(const FDocument& Document)
{
	return Document.GetObject();
}

TOptional<FValue::ConstMemberIterator> UnrealSharp::RapidJson::FindMember(FConstObject Object, FStringView FieldName)
{
	FValue::ConstMemberIterator FoundMember = Object.FindMember(FieldName.GetData());
	return FoundMember != Object.MemberEnd() ? FoundMember : TOptional<FValue::ConstMemberIterator>{};
}

TOptional<TStringView<TCHAR>> UnrealSharp::RapidJson::GetStringField(FConstObject Object, FStringView FieldName)
{
	TOptional<FValue::ConstMemberIterator> FoundMember = FindMember(Object, FieldName);
	
	if (!FoundMember.IsSet())
	{
		return {};
	}
	
	return TStringView(FoundMember.GetValue()->value.GetString(), FoundMember.GetValue()->value.GetStringLength());
}

TOptional<bool> UnrealSharp::RapidJson::GetBoolField(FConstObject Object, const TCHAR* FieldName)
{
	TOptional<FValue::ConstMemberIterator> FoundMember = FindMember(Object, FieldName);
	
	if (!FoundMember.IsSet())
	{
		return {};
	}
	
	return FoundMember.GetValue()->value.GetBool();
}

TOptional<int32> UnrealSharp::RapidJson::GetInt32Field(FConstObject Object, const TCHAR* FieldName)
{
	TOptional<FValue::ConstMemberIterator> FoundMember = FindMember(Object, FieldName);
	
	if (!FoundMember.IsSet())
	{
		return {};
	}
	
	return FoundMember.GetValue()->value.GetInt();
}

TOptional<uint32> UnrealSharp::RapidJson::GetUint32Field(FConstObject Object, const TCHAR* FieldName)
{
	TOptional<FValue::ConstMemberIterator> FoundMember = FindMember(Object, FieldName);
	
	if (!FoundMember.IsSet())
	{
		return {};
	}
	
	return FoundMember.GetValue()->value.GetUint();
}

TOptional<int64> UnrealSharp::RapidJson::GetInt64Field(FConstObject Object, const TCHAR* FieldName)
{
	TOptional<FValue::ConstMemberIterator> FoundMember = FindMember(Object, FieldName);
	
	if (!FoundMember.IsSet())
	{
		return {};
	}
	
	return FoundMember.GetValue()->value.GetInt64();
}

TOptional<uint64> UnrealSharp::RapidJson::GetUint64Field(FConstObject Object, const TCHAR* FieldName)
{
	TOptional<FValue::ConstMemberIterator> FoundMember = FindMember(Object, FieldName);
	
	if (!FoundMember.IsSet())
	{
		return {};
	}
	
	return FoundMember.GetValue()->value.GetUint64();
}

TOptional<float> UnrealSharp::RapidJson::GetFloatField(FConstObject Object, const TCHAR* FieldName)
{
	TOptional<FValue::ConstMemberIterator> FoundMember = FindMember(Object, FieldName);
	
	if (!FoundMember.IsSet())
	{
		return {};
	}
	
	return FoundMember.GetValue()->value.GetFloat();
}

TOptional<double> UnrealSharp::RapidJson::GetDoubleField(FConstObject Object, const TCHAR* FieldName)
{
	TOptional<FValue::ConstMemberIterator> FoundMember = FindMember(Object, FieldName);
	
	if (!FoundMember.IsSet())
	{
		return {};
	}
	
	return FoundMember.GetValue()->value.GetDouble();
}

TOptional<FConstObject> UnrealSharp::RapidJson::GetObjectField(FConstObject Object, const TCHAR* FieldName)
{
	TOptional<FValue::ConstMemberIterator> FoundMember = FindMember(Object, FieldName);
	
	if (!FoundMember.IsSet())
	{
		return {};
	}
	
	return FoundMember.GetValue()->value.GetObject();
}

TOptional<FConstArray> UnrealSharp::RapidJson::GetArrayField(FConstObject Object, const TCHAR* FieldName)
{
	TOptional<FValue::ConstMemberIterator> FoundMember = FindMember(Object, FieldName);
	
	if (!FoundMember.IsSet())
	{
		return {};
	}
	
	return FoundMember.GetValue()->value.GetArray();
}
