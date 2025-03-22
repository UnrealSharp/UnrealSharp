#pragma once

struct FCSMemberMetaData
{
	virtual ~FCSMemberMetaData() = default;

	FName Name;
	TMap<FString, FString> MetaData;
	
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject);

	bool HasMetaData(const FString& Key) const
	{
		return MetaData.Contains(Key);
	}

	bool operator == (const FCSMemberMetaData& Other) const
	{
		if (Name != Other.Name)
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
