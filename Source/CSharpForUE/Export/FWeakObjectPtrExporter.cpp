#include "FWeakObjectPtrExporter.h"

#include "CSharpForUE/CSManager.h"

void UFWeakObjectPtrExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(SetObject)
	EXPORT_FUNCTION(GetObject)
	EXPORT_FUNCTION(IsValid)
	EXPORT_FUNCTION(IsStale)
	EXPORT_FUNCTION(NativeEquals)
}

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
	return FCSManager::Get().FindManagedObject(Object).GetIntPtr();
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


