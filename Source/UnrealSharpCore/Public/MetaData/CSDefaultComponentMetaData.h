#pragma once

#include "FCSFieldTypePropertyMetaData.h"

struct FCSDefaultComponentMetaData : FCSFieldTypePropertyMetaData
{
	bool IsRootComponent = false;
	FName AttachmentComponent;
	FName AttachmentSocket;

	// FCSMetaDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override
	{
		START_JSON_SERIALIZE
		
		CALL_SERIALIZE(FCSFieldTypePropertyMetaData::Serialize(JsonObject));
		
		JSON_READ_BOOL(IsRootComponent, IS_REQUIRED);
		JSON_READ_STRING(AttachmentComponent, IS_REQUIRED);
		JSON_READ_STRING(AttachmentSocket, IS_REQUIRED);

		END_JSON_SERIALIZE
	}
	// End of FCSMetaDataBase interface

	bool HasValidAttachment() const { return AttachmentComponent != NAME_None; }
};
