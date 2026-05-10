#include "Json/CSJsonUtilities.h"

DEFINE_LOG_CATEGORY(LogCSJsonUtilties);

bool UnrealSharp::Json::ReadBoolField(bool& Destination, FConstObject Object, FStringView FieldName, bool bIsOptional)
{
    return ReadJsonField(GetBoolField(Object, FieldName.GetData()), bIsOptional, FieldName, [&Destination](bool V)
    {
        Destination = V;
        return true;
    });
}

bool UnrealSharp::Json::ReadIntField(int32& Destination, FConstObject Object, FStringView FieldName, bool bIsOptional)
{
    return ReadJsonField(GetInt32Field(Object, FieldName.GetData()), bIsOptional, FieldName, [&Destination](int32 V)
    {
        Destination = V; 
        return true;
    });
}

bool UnrealSharp::Json::ReadStringField(FString& Destination, FConstObject Object, FStringView FieldName, bool bIsOptional)
{
    return ReadJsonField(GetStringField(Object, FieldName.GetData()), bIsOptional, FieldName, [&Destination](FStringView V)
    {
        Destination = V; 
        return true;
    });
}

bool UnrealSharp::Json::ReadStringField(FName& Destination, FConstObject Object, FStringView FieldName, bool bIsOptional)
{
    return ReadJsonField(GetStringField(Object, FieldName.GetData()), bIsOptional, FieldName, [&Destination](FStringView V)
    {
        Destination = FName(V); 
        return true;
    });
}

bool UnrealSharp::Json::ReadStringArrayField(TArray<FName>& Destination, FConstObject Object, FStringView FieldName, bool bIsOptional)
{
    return ReadJsonField(GetArrayField(Object, FieldName.GetData()), bIsOptional, FieldName, [&Destination](const FConstArray& FieldArray) -> bool
    {
        Destination.Reset(FieldArray.Size());
        
        for (const FValue& Element : FieldArray)
        {
            Destination.Emplace(Element.GetString());
        }
        
        return true;
    });
}

bool UnrealSharp::Json::ReadStringArrayField(TArray<FString>& Destination, FConstObject Object, FStringView FieldName, bool bIsOptional)
{
    return ReadJsonField(GetArrayField(Object, FieldName.GetData()), bIsOptional, FieldName, [&Destination](const FConstArray& FieldArray) -> bool
    {
        Destination.Reset(FieldArray.Size());
        
        for (const FValue& Element : FieldArray)
        {
            Destination.Emplace(Element.GetString());
        }
        
        return true;
    });
}