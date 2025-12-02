#pragma once

#include "CoreMinimal.h"
#include "UnrealSharpCore.h"
#include "Dom/JsonObject.h"
#include "Logging/StructuredLog.h"

#define START_JSON_SERIALIZE bool bSuccess = true;
#define END_JSON_SERIALIZE return bSuccess;
#define CALL_SERIALIZE(SuperCall) bSuccess &= SuperCall;

#define SET_SUCCESS(Value) bSuccess = Value;

#define _GET_MACRO_2(_1,_2,NAME,...) NAME
#define _GET_MACRO_3(_1,_2,_3,NAME,...) NAME
#define _GET_MACRO_4(_1,_2,_3,_4,NAME,...) NAME

#define IS_OPTIONAL true
#define IS_REQUIRED false

#define JSON_READ_BOOL_1(MemberName, Optional) \
    bSuccess &= ReadBoolField(MemberName, JsonObject, TEXT(#MemberName), Optional);
#define JSON_READ_BOOL_2(CustomJson, MemberName, Optional) \
    bSuccess &= ReadBoolField(MemberName, CustomJson, TEXT(#MemberName), Optional);
#define JSON_READ_BOOL(...) \
    _GET_MACRO_3(__VA_ARGS__, JSON_READ_BOOL_2, JSON_READ_BOOL_1)(__VA_ARGS__)

#define JSON_READ_INT_1(MemberName, Optional) \
    bSuccess &= ReadIntField(MemberName, JsonObject, TEXT(#MemberName), Optional);
#define JSON_READ_INT_2(CustomJson, MemberName, Optional) \
    bSuccess &= ReadIntField(MemberName, CustomJson, TEXT(#MemberName), Optional);
#define JSON_READ_INT(...) \
    _GET_MACRO_3(__VA_ARGS__, JSON_READ_INT_2, JSON_READ_INT_1)(__VA_ARGS__)

#define JSON_READ_STRING_1(MemberName, Optional) \
    bSuccess &= ReadStringField(MemberName, JsonObject, TEXT(#MemberName), Optional);
#define JSON_READ_STRING_2(CustomJson, MemberName, Optional) \
    bSuccess &= ReadStringField(MemberName, CustomJson, TEXT(#MemberName), Optional);
#define JSON_READ_STRING(...) \
    _GET_MACRO_3(__VA_ARGS__, JSON_READ_STRING_2, JSON_READ_STRING_1)(__VA_ARGS__)

#define JSON_READ_STRING_ARRAY_1(MemberName, Optional) \
    bSuccess &= ReadStringArrayField(MemberName, JsonObject, TEXT(#MemberName), Optional);
#define JSON_READ_STRING_ARRAY_2(CustomJson, MemberName, Optional) \
    bSuccess &= ReadStringArrayField(MemberName, CustomJson, TEXT(#MemberName), Optional);
#define JSON_READ_STRING_ARRAY(...) \
    _GET_MACRO_3(__VA_ARGS__, JSON_READ_STRING_ARRAY_2, JSON_READ_STRING_ARRAY_1)(__VA_ARGS__)

#define JSON_READ_ENUM_2(MemberName, Optional) \
    bSuccess &= ReadFlags<decltype(MemberName)>(JsonObject, TEXT(#MemberName), MemberName, Optional);
#define JSON_READ_ENUM_3(CustomJson, MemberName, Optional) \
    bSuccess &= ReadFlags<decltype(MemberName)>(CustomJson, TEXT(#MemberName), MemberName, Optional);
#define JSON_READ_ENUM(...) \
    _GET_MACRO_3(__VA_ARGS__, JSON_READ_ENUM_3, JSON_READ_ENUM_2)(__VA_ARGS__)

#define JSON_PARSE_OBJECT_1(MemberName, Optional) \
    bSuccess &= ParseObjectField(MemberName, JsonObject, TEXT(#MemberName), Optional);
#define JSON_PARSE_OBJECT_2(CustomJson, MemberName, Optional) \
    bSuccess &= ParseObjectField(MemberName, CustomJson, TEXT(#MemberName), Optional);
#define JSON_PARSE_OBJECT(...) \
    _GET_MACRO_3(__VA_ARGS__, JSON_PARSE_OBJECT_2, JSON_PARSE_OBJECT_1)(__VA_ARGS__)

#define JSON_PARSE_OBJECT_ARRAY_1(MemberName, Optional) \
    bSuccess &= ParseObjectArrayField(MemberName, JsonObject, TEXT(#MemberName), Optional);
#define JSON_PARSE_OBJECT_ARRAY_2(CustomJson, MemberName, Optional) \
    bSuccess &= ParseObjectArrayField(MemberName, CustomJson, TEXT(#MemberName), Optional);
#define JSON_PARSE_OBJECT_ARRAY(...) \
    _GET_MACRO_3(__VA_ARGS__, JSON_PARSE_OBJECT_ARRAY_2, JSON_PARSE_OBJECT_ARRAY_1)(__VA_ARGS__)

struct FCSReflectionDataBase
{
	virtual ~FCSReflectionDataBase() = default;
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) = 0;

	static bool ReadBoolField(bool& Dest, const TSharedPtr<FJsonObject>& Object, const FString& FieldName, bool bIsOptional = false)
	{
		bool Value = false;
		
		if (!Object->TryGetBoolField(FieldName, Value))
		{
			if (bIsOptional)
			{
				return true;
			}
			
			UE_LOGFMT(LogUnrealSharp, Error, "Missing or invalid bool field '{0}'", *FieldName);
			return false;
		}

		Dest = Value;
		return true;
	}

	static bool ReadIntField(int32& Dest, const TSharedPtr<FJsonObject>& Object, const FString& FieldName, bool bIsOptional = false)
	{
		double NumberValue = 0.0;
		if (!Object->TryGetNumberField(FieldName, NumberValue))
		{
			if (bIsOptional)
			{
				return true;
			}
			
			UE_LOGFMT(LogUnrealSharp, Error, "Missing or invalid int field '{0}'", *FieldName);
			return false;
		}

		Dest = static_cast<int32>(NumberValue);
		return true;
	}

	static bool ReadStringField(FString& Destination, const TSharedPtr<FJsonObject>& Object, const FString& FieldName, bool bIsOptional = false)
	{
		FString Temp;
		if (!Object->TryGetStringField(FieldName, Temp))
		{
			if (bIsOptional)
			{
				return true;
			}
			
			UE_LOGFMT(LogUnrealSharp, Error, "Missing or invalid string field '{0}'", *FieldName);
			return false;
		}

		Destination = Temp;
		return true;
	}
	
	static bool ReadStringField(FName& Destination, const TSharedPtr<FJsonObject>& Object, const FString& FieldName, bool bIsOptional = false)
	{
		FString TempString;
		if (ReadStringField(TempString, Object, FieldName, bIsOptional))
		{
			Destination = FName(*TempString);
			return true;
		}
		
		return false;
	}

	static bool ReadStringArrayField(TArray<FName>& Destination, const TSharedPtr<FJsonObject>& Object, const FString& FieldName, bool bIsOptional = false)
	{
		TArray<FString> StringArray;
		if (!ReadStringArrayField(StringArray, Object, FieldName, bIsOptional))
		{
			return false;
		}

		Destination.Empty(StringArray.Num());
		
		for (const FString& String : StringArray)
		{
			Destination.Add(FName(*String));
		}
		
		return true;
	}

	static bool ReadStringArrayField(TArray<FString>& Destination, const TSharedPtr<FJsonObject>& Object, const FString& FieldName, bool bIsOptional = false)
	{
		const TArray<TSharedPtr<FJsonValue>>* FieldArray = nullptr;
		if (!Object->TryGetArrayField(FieldName, FieldArray) || !FieldArray)
		{
			if (bIsOptional)
			{
				return true;
			}
			
			UE_LOGFMT(LogUnrealSharp, Error, "Missing or invalid string array field '{0}'", *FieldName);
			return false;
		}

		Destination.Empty(FieldArray->Num());
		
		for (const TSharedPtr<FJsonValue>& Value : *FieldArray)
		{
			if (Value->Type != EJson::String)
			{
				UE_LOGFMT(LogUnrealSharp, Error, "Field '{0}' should be an array of strings", *FieldName);
				return false;
			}
			
			Destination.Add(Value->AsString());
		}
		
		return true;
	}

	template <class T>
	static bool ReadEnumField(T& Dest, const TSharedPtr<FJsonObject>& Object, const FString& FieldName)
	{
		FString TempString;
		if (!ReadStringField(TempString, Object, FieldName))
		{
			return false;
		}
		
		if (!MapFromString(Dest, TempString))
		{
			UE_LOGFMT(LogUnrealSharp, Error, "Failed to map enum from string '{0}' for field '{1}'", TempString, FieldName);
			return false;
		}
		
		return true;
	}

	template<typename FlagType>
	static bool ReadFlags(const TSharedPtr<FJsonObject>& Object, const FString& StringField, FlagType& OutFlags, bool bIsOptional = false)
	{
		FString FoundStringField;
		if (!Object->TryGetStringField(StringField, FoundStringField))
		{
			if (bIsOptional)
			{
				return true;
			}
			
			UE_LOGFMT(LogUnrealSharp, Error, "Missing or invalid flags field '{0}'", *StringField);
			return false;
		}

		uint64 FunctionFlagsInt;
		TTypeFromString<uint64>::FromString(FunctionFlagsInt, *FoundStringField);
		OutFlags = static_cast<FlagType>(FunctionFlagsInt);
		return true;
	};

	template <class T>
	static bool ParseObjectField(T& Dest, const TSharedPtr<FJsonObject>& Object, const FString& FieldName, bool bOptional = false)
	{
		const TSharedPtr<FJsonObject>* ChildObject;
		if (!Object->TryGetObjectField(FieldName, ChildObject) || !ChildObject)
		{
			if (bOptional)
			{
				return true;
			}
			
			UE_LOGFMT(LogUnrealSharp, Error, "Missing or invalid object field '{0}'", *FieldName);
			return false;
		}
		
		return Dest.Serialize(*ChildObject);
	}

	template <class T>
	static bool ParseObjectArrayField(TArray<T>& Dest, const TSharedPtr<FJsonObject>& Object, const FString& FieldName, bool bOptional = false)
	{
		START_JSON_SERIALIZE
		
		const TArray<TSharedPtr<FJsonValue>>* FieldArray = nullptr;
		if (!Object->TryGetArrayField(FieldName, FieldArray) || !FieldArray)
		{
			if (bOptional)
			{
				return true;
			}
			
			UE_LOGFMT(LogUnrealSharp, Error, "Missing or invalid object array field '{0}'", *FieldName);
			return false;
		}

		Dest.Empty(FieldArray->Num());
		for (const auto& Value : *FieldArray)
		{
			if (Value->Type != EJson::Object)
			{
				UE_LOGFMT(LogUnrealSharp, Error, "Field '{0}' should be an array of objects", *FieldName);
				return false;
			}

			T& NewItem = Dest.Emplace_GetRef();
			CALL_SERIALIZE(NewItem.Serialize(Value->AsObject()));
		}

		END_JSON_SERIALIZE
	}
};
