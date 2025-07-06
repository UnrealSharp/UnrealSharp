#pragma once

#include "CSFieldName.h"

struct FCSAssembly;

struct FCSTypeReferenceMetaData
{
	virtual ~FCSTypeReferenceMetaData() = default;
	FCSTypeReferenceMetaData();

	FCSFieldName FieldName;
	FName AssemblyName;

	TSharedPtr<FCSAssembly> GetOwningAssemblyChecked() const;
	
	UClass* GetOwningClass() const;
	UScriptStruct* GetOwningStruct() const;
	UEnum* GetOwningEnum() const;
	UClass* GetOwningInterface() const;
	UDelegateFunction* GetOwningDelegate() const;
	UPackage* GetOwningPackage() const;

	TMap<FString, FString> MetaData;
	
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject);

	bool operator==(const FCSTypeReferenceMetaData& Other) const
	{
		if (FieldName != Other.FieldName || AssemblyName != Other.AssemblyName)
		{
			return false;
		}

		for (const TPair<FString, FString>& Pair : MetaData)
		{
			if (!Other.MetaData.Contains(Pair.Key) || Other.MetaData[Pair.Key] != Pair.Value)
			{
				return false;
			}
		}
		
		return true;
	}
	
	friend uint32 GetTypeHash(const FCSTypeReferenceMetaData& Type)
    {
        return GetTypeHash(Type.FieldName) ^ GetTypeHash(Type.AssemblyName);
    }
};
