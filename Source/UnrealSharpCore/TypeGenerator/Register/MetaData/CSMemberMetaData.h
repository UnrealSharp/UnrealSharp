#pragma once

struct FCSMemberMetaData
{
	virtual ~FCSMemberMetaData() = default;

	FName Name;
	TMap<FString, FString> MetaData;
	
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject);;
};
