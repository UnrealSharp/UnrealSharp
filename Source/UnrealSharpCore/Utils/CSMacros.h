#pragma once

#define DECLARE_CSHARP_TYPE_FUNCTIONS(TypeInfoStruct) \
public: \
	void SetTypeInfo(const TSharedPtr<struct TypeInfoStruct>& InTypeInfo) { TypeInfo = InTypeInfo; } \
	TSharedPtr<struct TypeInfoStruct> GetTypeInfo() const { return TypeInfo; } \
	bool HasTypeInfo() const { return TypeInfo.IsValid(); } \
private: \
	TSharedPtr<struct TypeInfoStruct> TypeInfo; \
