#pragma once

#include "CSFieldType.h"

struct FCSDefaultComponentType : FCSFieldType
{
	// FCSReflectionDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override;
	// End of FCSReflectionDataBase interface

	bool HasValidAttachment() const { return AttachmentComponent != NAME_None; }

	bool IsRootComponent = false;
	FName AttachmentComponent;
	FName AttachmentSocket;
};
