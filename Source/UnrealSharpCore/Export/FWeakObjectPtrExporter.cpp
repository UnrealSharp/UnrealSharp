#include "FWeakObjectPtrExporter.h"
#include "UnrealSharpCore/CSManager.h"

void UFWeakObjectPtrExporter::SetObject(TWeakObjectPtr<UObject>& WeakObject, UObject* Object)
{
	WeakObject = Object;
}

void* UFWeakObjectPtrExporter::GetObject(TWeakObjectPtr<UObject> WeakObjectPtr)
{
	if (!WeakObjectPtr.IsValid())
	{
		return nullptr;
	}

	UObject* Object = WeakObjectPtr.Get();
	return UCSManager::Get().FindManagedObject(Object);
}

bool UFWeakObjectPtrExporter::IsValid(TWeakObjectPtr<UObject> WeakObjectPtr)
{
	return WeakObjectPtr.IsValid();
}

bool UFWeakObjectPtrExporter::IsStale(TWeakObjectPtr<UObject> WeakObjectPtr)
{
	return WeakObjectPtr.IsStale();
}

bool UFWeakObjectPtrExporter::NativeEquals(TWeakObjectPtr<UObject> A, TWeakObjectPtr<UObject> B)
{
	return A == B;
}


