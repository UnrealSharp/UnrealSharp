#pragma once

struct FCSAssembly;

struct FCSTypeReferenceMetaData
{
	virtual ~FCSTypeReferenceMetaData() = default;

	FName Name;
	FName Namespace;
	FName AssemblyName;

	TSharedPtr<FCSAssembly> GetOwningAssemblyChecked() const;
	
	UClass* GetOwningClass() const;
	UScriptStruct* GetOwningStruct() const;
	UEnum* GetOwningEnum() const;
	UClass* GetOwningInterface() const;

	TMap<FString, FString> MetaData;
	
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject);

	bool operator==(const FCSTypeReferenceMetaData& Other) const
	{
		if (Name != Other.Name || Namespace != Other.Namespace || AssemblyName != Other.AssemblyName)
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
        return GetTypeHash(Type.Name) ^ GetTypeHash(Type.Namespace) ^ GetTypeHash(Type.AssemblyName);
    }
};
