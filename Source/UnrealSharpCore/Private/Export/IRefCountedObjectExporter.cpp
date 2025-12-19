#include "Export/IRefCountedObjectExporter.h"

uint32 UIRefCountedObjectExporter::GetRefCount(const IRefCountedObject* Object)
{
	if (!Object || Object->GetRefCount() == 0)
	{
		return 0;
	}
	
	return Object->GetRefCount();
}

uint32 UIRefCountedObjectExporter::AddRef(const IRefCountedObject* Object)
{
	if (!Object || Object->GetRefCount() == 0)
	{
		return 0;
	}
	
	Object->AddRef();
	return Object->GetRefCount();
}

uint32 UIRefCountedObjectExporter::Release(const IRefCountedObject* Object)
{
	if (!Object || Object->GetRefCount() == 0)
	{
		return 0;
	}
	
	return Object->Release();
}
