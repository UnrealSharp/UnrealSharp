#pragma once
#include "CSObjectMetaData.h"

struct FCSDefaultComponentMetaData : public FCSObjectMetaData
{
	virtual ~FCSDefaultComponentMetaData() = default;

	bool IsRootComponent = false;
	FName AttachmentComponent;
	FName AttachmentSocket;

	//FUnrealType interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	virtual bool IsEqual(TSharedPtr<FCSUnrealType> Other) const override;
	//End of implementation
};
