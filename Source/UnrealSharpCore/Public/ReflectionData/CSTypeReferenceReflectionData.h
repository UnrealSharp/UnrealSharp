#pragma once

#include "CSFieldName.h"
#include "CSReflectionDataBase.h"

class UCSManagedTypeCompiler;
class UCSManagedAssembly;

struct FCSMetaDataEntry : FCSReflectionDataBase
{
	FCSMetaDataEntry(const FString& InKey, const FString& InValue = FString())
		: Key(InKey)
		, Value(InValue)
	{
	}

	FCSMetaDataEntry() {}

	// FCSReflectionDataBase interface
	virtual bool Serialize(FConstObject JsonObject) override;
	// End of FCSReflectionDataBase interface

	FString Key;
	FString Value;
};

struct FCSTypeReferenceReflectionData : FCSReflectionDataBase
{
	void SerializeFromJsonString(TCHAR* RawJsonString);
	
	// FCSReflectionDataBase interface
	virtual bool Serialize(FConstObject JsonObject) override;
	// End of FCSReflectionDataBase interface

	bool HasMetaData(const FString& Key) const
	{
		for (const FCSMetaDataEntry& MetaDataEntry : MetaData)
		{
			if (MetaDataEntry.Key == Key)
			{
				return true;
			}
		}
		return false;
	}

	friend uint32 GetTypeHash(const FCSTypeReferenceReflectionData& Type)
	{
		return GetTypeHash(Type.FieldName);
	}

	bool operator==(const FCSTypeReferenceReflectionData& Other) const
	{
		return FieldName != Other.FieldName;
	}
	
	FCSFieldName FieldName;
	TArray<FCSMetaDataEntry> MetaData;
	TArray<FCSFieldName> Dependencies;
	const FString& GetRawReflectionData() const { return RawReflectionData; }
	
private:
	FString RawReflectionData;
};
