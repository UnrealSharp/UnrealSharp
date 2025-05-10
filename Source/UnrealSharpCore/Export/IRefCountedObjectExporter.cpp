// Fill out your copyright notice in the Description page of Project Settings.


#include "IRefCountedObjectExporter.h"

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
	
	return Object->AddRef();
}

uint32 UIRefCountedObjectExporter::Release(const IRefCountedObject* Object)
{
	if (!Object || Object->GetRefCount() == 0)
	{
		return 0;
	}
	
	return Object->Release();
}
