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
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override;
	// End of FCSReflectionDataBase interface

	FString Key;
	FString Value;
};

struct FCSTypeReferenceReflectionData : FCSReflectionDataBase
{
	// FCSReflectionDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override;
	// End of FCSReflectionDataBase interface

	UCSManagedAssembly* GetOwningAssemblyChecked() const;
	
	UClass* GetAsClass() const;
	UScriptStruct* GetAsStruct() const;
	UEnum* GetAsEnum() const;
	UClass* GetAsInterface() const;
	UDelegateFunction* GetAsDelegate() const;
	UPackage* GetAsPackage() const;
	
	bool IsValid() const { return FieldName.IsValid() && AssemblyName != NAME_None; }

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
		return GetTypeHash(Type.FieldName) ^ GetTypeHash(Type.AssemblyName);
	}

	bool operator==(const FCSTypeReferenceReflectionData& Other) const
	{
		return FieldName != Other.FieldName || AssemblyName != Other.AssemblyName;
	}

	FCSFieldName FieldName;
	FName AssemblyName;
	TArray<FCSMetaDataEntry> MetaData;
	TArray<FCSFieldName> SourceGeneratorDependencies;
};
