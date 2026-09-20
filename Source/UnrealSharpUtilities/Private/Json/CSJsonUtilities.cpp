#include "Json/CSJsonUtilities.h"

DEFINE_LOG_CATEGORY(LogUnrealSharpRapidJson);

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
    return ReadJsonField(GetInt32Field(Object, FieldName.GetData()), bIsOptional, FieldName, [&Destination](int32 Value)
    {
        Destination = Value; 
        return true;
    });
}

bool UnrealSharp::Json::ReadStringField(FString& Destination, FConstObject Object, FStringView FieldName, bool bIsOptional)
{
    return ReadJsonField(GetStringField(Object, FieldName.GetData()), bIsOptional, FieldName, [&Destination](FStringView Value)
    {
        Destination = Value; 
        return true;
    });
}

bool UnrealSharp::Json::ReadStringField(FName& Destination, FConstObject Object, FStringView FieldName, bool bIsOptional)
{
    return ReadJsonField(GetStringField(Object, FieldName.GetData()), bIsOptional, FieldName, [&Destination](FStringView Value)
    {
        Destination = FName(Value); 
        return true;
    });
}

bool UnrealSharp::Json::ReadStringArrayField(TArray<FName>& Destination, FConstObject Object, FStringView FieldName, bool bIsOptional)
{
    return ReadJsonField(GetArrayField(Object, FieldName.GetData()), bIsOptional, FieldName, [&Destination](const FConstArray& Value) -> bool
    {
        Destination.Reset(Value.Size());
        
        for (const FValue& Element : Value)
        {
            Destination.Emplace(Element.GetString());
        }
        
        return true;
    });
}

bool UnrealSharp::Json::ReadStringArrayField(TArray<FString>& Destination, FConstObject Object, FStringView FieldName, bool bIsOptional)
{
    return ReadJsonField(GetArrayField(Object, FieldName.GetData()), bIsOptional, FieldName, [&Destination](const FConstArray& Value) -> bool
    {
        Destination.Reset(Value.Size());
        
        for (const FValue& Element : Value)
        {
            Destination.Emplace(Element.GetString());
        }
        
        return true;
    });
}