#pragma once

#include "CSFieldTypePropertyMetaData.h"

struct FCSDefaultComponentMetaData : FCSFieldTypePropertyMetaData
{
	// FCSMetaDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override;
	// End of FCSMetaDataBase interface

	bool HasValidAttachment() const { return AttachmentComponent != NAME_None; }

	bool IsRootComponent = false;
	FName AttachmentComponent;
	FName AttachmentSocket;
};
