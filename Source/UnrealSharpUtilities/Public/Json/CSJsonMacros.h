#pragma once

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
    bSuccess &= UnrealSharp::Json::ReadBoolField(MemberName, JsonObject, TEXT(#MemberName), Optional);
#define JSON_READ_BOOL_2(CustomJson, MemberName, Optional) \
    bSuccess &= UnrealSharp::Json::ReadBoolField(MemberName, CustomJson, TEXT(#MemberName), Optional);
#define JSON_READ_BOOL(...) \
    _GET_MACRO_3(__VA_ARGS__, JSON_READ_BOOL_2, JSON_READ_BOOL_1)(__VA_ARGS__)

#define JSON_READ_INT_1(MemberName, Optional) \
    bSuccess &= UnrealSharp::Json::ReadIntField(MemberName, JsonObject, TEXT(#MemberName), Optional);
#define JSON_READ_INT_2(CustomJson, MemberName, Optional) \
    bSuccess &= UnrealSharp::Json::ReadIntField(MemberName, CustomJson, TEXT(#MemberName), Optional);
#define JSON_READ_INT(...) \
    _GET_MACRO_3(__VA_ARGS__, JSON_READ_INT_2, JSON_READ_INT_1)(__VA_ARGS__)

#define JSON_READ_STRING_1(MemberName, Optional) \
    bSuccess &= UnrealSharp::Json::ReadStringField(MemberName, JsonObject, TEXT(#MemberName), Optional);
#define JSON_READ_STRING_2(CustomJson, MemberName, Optional) \
    bSuccess &= UnrealSharp::Json::ReadStringField(MemberName, CustomJson, TEXT(#MemberName), Optional);
#define JSON_READ_STRING(...) \
    _GET_MACRO_3(__VA_ARGS__, JSON_READ_STRING_2, JSON_READ_STRING_1)(__VA_ARGS__)

#define JSON_READ_STRING_ARRAY_1(MemberName, Optional) \
    bSuccess &= UnrealSharp::Json::ReadStringArrayField(MemberName, JsonObject, TEXT(#MemberName), Optional);
#define JSON_READ_STRING_ARRAY_2(CustomJson, MemberName, Optional) \
    bSuccess &= UnrealSharp::Json::ReadStringArrayField(MemberName, CustomJson, TEXT(#MemberName), Optional);
#define JSON_READ_STRING_ARRAY(...) \
    _GET_MACRO_3(__VA_ARGS__, JSON_READ_STRING_ARRAY_2, JSON_READ_STRING_ARRAY_1)(__VA_ARGS__)

#define JSON_READ_ENUM_2(MemberName, Optional) \
    bSuccess &= UnrealSharp::Json::ReadFlags<decltype(MemberName)>(JsonObject, TEXT(#MemberName), MemberName, Optional);
#define JSON_READ_ENUM_3(CustomJson, MemberName, Optional) \
    bSuccess &= UnrealSharp::Json::ReadFlags<decltype(MemberName)>(CustomJson, TEXT(#MemberName), MemberName, Optional);
#define JSON_READ_ENUM(...) \
    _GET_MACRO_3(__VA_ARGS__, JSON_READ_ENUM_3, JSON_READ_ENUM_2)(__VA_ARGS__)

#define JSON_PARSE_OBJECT_1(MemberName, Optional) \
    bSuccess &= UnrealSharp::Json::ParseObjectField(MemberName, JsonObject, TEXT(#MemberName), Optional);
#define JSON_PARSE_OBJECT_2(CustomJson, MemberName, Optional) \
    bSuccess &= UnrealSharp::Json::ParseObjectField(MemberName, CustomJson, TEXT(#MemberName), Optional);
#define JSON_PARSE_OBJECT(...) \
    _GET_MACRO_3(__VA_ARGS__, JSON_PARSE_OBJECT_2, JSON_PARSE_OBJECT_1)(__VA_ARGS__)

#define JSON_PARSE_OBJECT_ARRAY_1(MemberName, Optional) \
    bSuccess &= UnrealSharp::Json::ParseObjectArrayField(MemberName, JsonObject, TEXT(#MemberName), Optional);
#define JSON_PARSE_OBJECT_ARRAY_2(CustomJson, MemberName, Optional) \
    bSuccess &= UnrealSharp::Json::ParseObjectArrayField(MemberName, CustomJson, TEXT(#MemberName), Optional);
#define JSON_PARSE_OBJECT_ARRAY(...) \
    _GET_MACRO_3(__VA_ARGS__, JSON_PARSE_OBJECT_ARRAY_2, JSON_PARSE_OBJECT_ARRAY_1)(__VA_ARGS__)