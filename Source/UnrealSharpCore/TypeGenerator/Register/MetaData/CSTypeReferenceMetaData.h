#pragma once

struct FCSTypeReferenceMetaData
{
	virtual ~FCSTypeReferenceMetaData() = default;

	FName Name;
	FName Namespace;
	FName AssemblyName;

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
};
