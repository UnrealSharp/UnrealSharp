#pragma once

#include "CoreMinimal.h"
#include "CSRapidJsonUtilties.h"
#include "Logging/StructuredLog.h"

UNREALSHARPUTILITIES_API DECLARE_LOG_CATEGORY_EXTERN(LogUnrealSharpRapidJson, Log, All);

using namespace UnrealSharp::RapidJson;

namespace UnrealSharp::Json
{
    template <typename T, typename FOnFound>
    FORCEINLINE bool ReadJsonField(TOptional<T> Value, bool bIsOptional, FStringView FieldName, FOnFound&& OnFound)
    {
        if (Value.IsSet())
        {
            return OnFound(*Value);
        }
        
        if (bIsOptional)
        {
            return true;
        }
        
        UE_LOGFMT(LogUnrealSharpRapidJson, Fatal, "Missing or invalid field '{1}'", FieldName);
    }
    
    UNREALSHARPUTILITIES_API bool ReadBoolField(bool& Destination, FConstObject Object, FStringView FieldName, bool bIsOptional = false);
    UNREALSHARPUTILITIES_API bool ReadIntField(int32& Destination, FConstObject Object, FStringView FieldName, bool bIsOptional = false);
    UNREALSHARPUTILITIES_API bool ReadStringField(FString& Destination, FConstObject Object, FStringView FieldName, bool bIsOptional = false);
    UNREALSHARPUTILITIES_API bool ReadStringField(FName& Destination, FConstObject Object, FStringView FieldName, bool bIsOptional = false);
    UNREALSHARPUTILITIES_API bool ReadStringArrayField(TArray<FName>& Destination, FConstObject Object, FStringView FieldName, bool bIsOptional = false);
    UNREALSHARPUTILITIES_API bool ReadStringArrayField(TArray<FString>& Destination, FConstObject Object, FStringView FieldName, bool bIsOptional = false);

    template <class T>
    bool ReadEnumField(T& Dest, FConstObject Object, FStringView FieldName, bool bIsOptional = false)
    {
        return ReadJsonField(GetStringField(Object, FieldName.GetData()), bIsOptional, FieldName, [&Dest, FieldName](FStringView Value) -> bool
        {
            const FString TempString(Value);
            
            if (!MapFromString(Dest, TempString))
            {
                UE_LOGFMT(LogUnrealSharpRapidJson, Error, "Failed to map enum from string '{0}' for field '{1}'", TempString, FieldName);
                return false;
            }
            
            return true;
        });
    }

    template <typename FlagType>
    bool ReadFlags(FConstObject Object, FStringView FieldName, FlagType& OutFlags, bool bIsOptional = false)
    {
        return ReadJsonField(GetInt64Field(Object, FieldName.GetData()), bIsOptional, FieldName, [&OutFlags](int64 Value)
        {
            OutFlags = static_cast<FlagType>(Value); 
            return true;
        });
    }

    template <class T>
    bool ParseObjectField(T& Dest, FConstObject Object, FStringView FieldName, bool bOptional = false)
    {
        return ReadJsonField(GetObjectField(Object, FieldName.GetData()),bOptional, FieldName, [&Dest](FConstObject Value)
        {
            return Dest.Serialize(Value);
        });
    }

    template <class T>
    bool ParseObjectArrayField(TArray<T>& Dest, FConstObject Object, FStringView FieldName, bool bOptional = false)
    {
        return ReadJsonField(GetArrayField(Object, FieldName.GetData()), bOptional, FieldName, [&Dest](const FConstArray& Value) -> bool
        {
            Dest.Reset(Value.Size());
            
            for (const FValue& Element : Value)
            {
                T& NewItem = Dest.Emplace_GetRef();
                if (!NewItem.Serialize(Element.GetObject()))
                {
                    return false;
                }
            }
            
            return true;
        });
    }
}