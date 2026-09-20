#include "Json/CSRapidJsonUtilties.h"
#include "Json/CSJsonUtilities.h"
#include "rapidjson/error/en.h"

namespace UnrealSharp::RapidJson
{
	namespace
	{
		const TCHAR* GetTypeName(const FValue& Value)
		{
			switch (Value.GetType())
			{
			case rapidjson::kNullType: return TEXT("null");
			case rapidjson::kFalseType:
			case rapidjson::kTrueType: return TEXT("bool");
			case rapidjson::kObjectType: return TEXT("object");
			case rapidjson::kArrayType: return TEXT("array");
			case rapidjson::kStringType: return TEXT("string");
			case rapidjson::kNumberType: return TEXT("number");
			}

			return TEXT("unknown");
		}

		void LogTypeMismatch(FStringView FieldName, const TCHAR* ExpectedType, const FValue& Value)
		{
			UE_LOGFMT(LogUnrealSharpRapidJson, Warning, "Field '{0}' is a {1}, expected {2}", FieldName,
			          GetTypeName(Value), ExpectedType);
		}

		void LogInvalidValue(FStringView FieldName, const TCHAR* ExpectedType)
		{
			UE_LOGFMT(LogUnrealSharpRapidJson, Warning, "Field '{0}' has a value that is not a valid {1}", FieldName,
			          ExpectedType);
		}

		const FValue* FindValue(const FConstObject& Object, FStringView FieldName)
		{
			TOptional<FValue::ConstMemberIterator> Member = FindMember(Object, FieldName);

			if (!Member.IsSet())
			{
				return nullptr;
			}

			return &Member.GetValue()->value;
		}

		const FValue* FindTypedValue(const FConstObject& Object, FStringView FieldName, bool (FValue::*IsType)() const,
		                             const TCHAR* ExpectedType)
		{
			const FValue* Value = FindValue(Object, FieldName);

			if (Value && !(Value->*IsType)())
			{
				LogTypeMismatch(FieldName, ExpectedType, *Value);
				return nullptr;
			}

			return Value;
		}

		template <typename T, typename U>
		TOptional<T> CastInRange(U Value)
		{
			if (!std::in_range<T>(Value))
			{
				return {};
			}

			return static_cast<T>(Value);
		}

		template <typename T>
		TOptional<T> ParseIntegerString(FStringView Str)
		{
			const bool bNegative = !Str.IsEmpty() && Str[0] == TEXT('-');

			if (bNegative)
			{
				Str.RightChopInline(1);
			}

			if (Str.IsEmpty())
			{
				return {};
			}

			uint64 Magnitude = 0;

			for (TCHAR Char : Str)
			{
				if (Char < TEXT('0') || Char > TEXT('9'))
				{
					return {};
				}

				const uint64 Digit = Char - TEXT('0');

				if (Magnitude > (std::numeric_limits<uint64>::max() - Digit) / 10)
				{
					return {};
				}

				Magnitude = Magnitude * 10 + Digit;
			}

			if (!bNegative)
			{
				return CastInRange<T>(Magnitude);
			}
			
			if (Magnitude > static_cast<uint64>(std::numeric_limits<int64>::max()) + 1)
			{
				return {};
			}
			
			return CastInRange<T>(static_cast<int64>(0 - Magnitude));
		}

		template <typename T>
		TOptional<T> GetIntegerField(FConstObject Object, FStringView FieldName, const TCHAR* ExpectedType)
		{
			const FValue* Value = FindValue(Object, FieldName);

			if (!Value)
			{
				return {};
			}

			TOptional<T> Result;

			if (Value->IsInt64())
			{
				Result = CastInRange<T>(Value->GetInt64());
			}
			else if (Value->IsUint64())
			{
				Result = CastInRange<T>(Value->GetUint64());
			}
			else if (Value->IsString())
			{
				Result = ParseIntegerString<T>(FStringView(Value->GetString(), Value->GetStringLength()));
			}
			else
			{
				LogTypeMismatch(FieldName, ExpectedType, *Value);
				return {};
			}

			if (!Result.IsSet())
			{
				LogInvalidValue(FieldName, ExpectedType);
			}

			return Result;
		}
	}

	bool ParseJsonString(TCHAR* JsonText, FDocument& OutDocument)
	{
		rapidjson::GenericInsituStringStream<FEncoding> Stream(JsonText);

		FDocument Result;
		Result.ParseStream<rapidjson::kParseInsituFlag>(Stream);

		if (Result.HasParseError())
		{
			UE_LOGFMT(LogUnrealSharpRapidJson, Error, "Failed to parse JSON at offset {0}: {1}",
			          static_cast<uint64>(Result.GetErrorOffset()),
			          FString(ANSI_TO_TCHAR(rapidjson::GetParseError_En(Result.GetParseError()))));
			return false;
		}

		OutDocument = MoveTemp(Result);
		return true;
	}

	TOptional<FConstObject> GetRootObject(const FDocument& Document)
	{
		if (!Document.IsObject())
		{
			UE_LOGFMT(LogUnrealSharpRapidJson, Error, "JSON root is a {0}, expected object", GetTypeName(Document));
			return {};
		}

		return Document.GetObject();
	}

	TOptional<FValue::ConstMemberIterator> FindMember(const FConstObject& Object, FStringView FieldName)
	{
		const FValue Name(rapidjson::StringRef(FieldName.GetData(), static_cast<rapidjson::SizeType>(FieldName.Len())));
		FValue::ConstMemberIterator FoundMember = Object.FindMember(Name);

		if (FoundMember == Object.MemberEnd())
		{
			return {};
		}

		return FoundMember;
	}

	TOptional<FStringView> GetStringField(const FConstObject& Object, FStringView FieldName)
	{
		const FValue* Value = FindTypedValue(Object, FieldName, &FValue::IsString, TEXT("string"));

		if (!Value)
		{
			return {};
		}

		return FStringView(Value->GetString(), Value->GetStringLength());
	}

	TOptional<bool> GetBoolField(const FConstObject& Object, FStringView FieldName)
	{
		const FValue* Value = FindTypedValue(Object, FieldName, &FValue::IsBool, TEXT("bool"));

		if (!Value)
		{
			return {};
		}

		return Value->GetBool();
	}

	TOptional<int32> GetInt32Field(const FConstObject& Object, FStringView FieldName)
	{
		return GetIntegerField<int32>(Object, FieldName, TEXT("int32"));
	}

	TOptional<uint32> GetUint32Field(const FConstObject& Object, FStringView FieldName)
	{
		return GetIntegerField<uint32>(Object, FieldName, TEXT("uint32"));
	}

	TOptional<int64> GetInt64Field(const FConstObject& Object, FStringView FieldName)
	{
		return GetIntegerField<int64>(Object, FieldName, TEXT("int64"));
	}

	TOptional<uint64> GetUint64Field(const FConstObject& Object, FStringView FieldName)
	{
		return GetIntegerField<uint64>(Object, FieldName, TEXT("uint64"));
	}

	TOptional<double> GetDoubleField(const FConstObject& Object, FStringView FieldName)
	{
		const FValue* Value = FindTypedValue(Object, FieldName, &FValue::IsNumber, TEXT("number"));

		if (!Value)
		{
			return {};
		}

		return Value->GetDouble();
	}

	TOptional<float> GetFloatField(const FConstObject& Object, FStringView FieldName)
	{
		TOptional<double> Value = GetDoubleField(Object, FieldName);

		if (!Value.IsSet())
		{
			return {};
		}

		return static_cast<float>(Value.GetValue());
	}

	TOptional<FConstObject> GetObjectField(const FConstObject& Object, FStringView FieldName)
	{
		const FValue* Value = FindTypedValue(Object, FieldName, &FValue::IsObject, TEXT("object"));

		if (!Value)
		{
			return {};
		}

		return Value->GetObject();
	}

	TOptional<FConstArray> GetArrayField(const FConstObject& Object, FStringView FieldName)
	{
		const FValue* Value = FindTypedValue(Object, FieldName, &FValue::IsArray, TEXT("array"));

		if (!Value)
		{
			return {};
		}

		return Value->GetArray();
	}
}
