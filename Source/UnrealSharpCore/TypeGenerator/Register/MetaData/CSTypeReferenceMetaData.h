#pragma once

struct FCSTypeReferenceMetaData
{
	virtual ~FCSTypeReferenceMetaData() = default;

	FName Name;
	FName Namespace;
	FName AssemblyName;

	TMap<FString, FString> MetaData;
	
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject);
};
