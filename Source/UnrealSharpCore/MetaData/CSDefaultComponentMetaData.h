#pragma once

#include "FCSFieldTypePropertyMetaData.h"

struct FCSDefaultComponentMetaData : FCSFieldTypePropertyMetaData
{
	bool IsRootComponent = false;
	FName AttachmentComponent;
	FName AttachmentSocket;

	bool HasValidAttachment() const { return AttachmentComponent != NAME_None; }
};
