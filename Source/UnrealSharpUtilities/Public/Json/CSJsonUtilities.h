#pragma once

#include "CoreMinimal.h"
#include "CSRapidJsonUtilties.h"
#include "Logging/StructuredLog.h"

UNREALSHARPUTILITIES_API DECLARE_LOG_CATEGORY_EXTERN(LogCSJsonUtilties, Log, All);

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
            
        UE_LOGFMT(LogCSJsonUtilties, Error, "Missing or invalid {0} field '{1}'", FieldName);
        return false;
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
        return ReadJsonField(GetStringField(Object, FieldName.GetData()), bIsOptional, FieldName, [&Dest, FieldName](FStringView View) -> bool
        {
            const FString TempString(View);
            
            if (!MapFromString(Dest, TempString))
            {
                UE_LOGFMT(LogCSJsonUtilties, Error, "Failed to map enum from string '{0}' for field '{1}'", TempString, FieldName);
                return false;
            }
            
            return true;
        });
    }

    template <typename FlagType>
    bool ReadFlags(FConstObject Object, FStringView FieldName, FlagType& OutFlags, bool bIsOptional = false)
    {
        return ReadJsonField(GetInt64Field(Object, FieldName.GetData()), bIsOptional, FieldName, [&OutFlags](int64 V)
        {
            OutFlags = static_cast<FlagType>(V); 
            return true;
        });
    }

    template <class T>
    bool ParseObjectField(T& Dest, FConstObject Object, FStringView FieldName, bool bOptional = false)
    {
        return ReadJsonField(GetObjectField(Object, FieldName.GetData()),bOptional, FieldName, [&Dest](FConstObject FieldObject)
        {
            return Dest.Serialize(FieldObject);
        });
    }

    template <class T>
    bool ParseObjectArrayField(TArray<T>& Dest, FConstObject Object, FStringView FieldName, bool bOptional = false)
    {
        return ReadJsonField(GetArrayField(Object, FieldName.GetData()), bOptional, FieldName, [&Dest](const FConstArray& FieldArray) -> bool
        {
            Dest.Reset(FieldArray.Size());
            
            for (const FValue& Element : FieldArray)
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