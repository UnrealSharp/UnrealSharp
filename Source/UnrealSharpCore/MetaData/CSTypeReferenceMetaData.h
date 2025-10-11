#pragma once

#include "CSFieldName.h"

class UCSAssembly;

struct FCSTypeReferenceMetaData
{
	virtual ~FCSTypeReferenceMetaData() = default;

	FCSTypeReferenceMetaData()
	{
		FieldClass = nullptr;
		AssemblyName = NAME_None;
		FieldName = FCSFieldName();
	}
	
	FCSFieldName FieldName;
	
	FName AssemblyName;
	
	UClass* FieldClass;
	
	TMap<FString, FString> MetaData;

	UCSAssembly* GetOwningAssemblyChecked() const;
	
	UClass* GetAsClass() const;
	UScriptStruct* GetAsStruct() const;
	UEnum* GetAsEnum() const;
	UClass* GetAsInterface() const;
	UDelegateFunction* GetAsDelegate() const;
	UPackage* GetAsPackage() const;
	
	bool IsValid() const { return FieldName.IsValid() && AssemblyName != NAME_None; }

	bool HasMetaData(const FString& Key) const
	{
		return MetaData.Contains(Key);
	}

	friend uint32 GetTypeHash(const FCSTypeReferenceMetaData& Type)
	{
		return GetTypeHash(Type.FieldName) ^ GetTypeHash(Type.AssemblyName);
	}

	bool operator==(const FCSTypeReferenceMetaData& Other) const
	{
		return FieldName != Other.FieldName || AssemblyName != Other.AssemblyName;
	}
};
