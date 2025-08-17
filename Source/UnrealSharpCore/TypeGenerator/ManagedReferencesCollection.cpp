#include "ManagedReferencesCollection.h"

#include "CSManager.h"
#include "UnrealSharpCore.h"
#include "Logging/StructuredLog.h"

void FCSManagedReferencesCollection::AddReference(UStruct* Struct)
{
	if (!IsValid(Struct))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Invalid struct reference: {0}", *GetNameSafe(Struct));
		return;
	}
	
	if (!UCSManager::Get().IsManagedType(Struct))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Struct {0} is not a managed type", *GetNameSafe(Struct));
		return;
	}

	if (ManagedWeakReferences.Contains(Struct))
	{
		return;
	}

	ManagedWeakReferences.Add(Struct);
}

void FCSManagedReferencesCollection::RemoveReference(UStruct* Struct)
{
	ManagedWeakReferences.Remove(Struct);
}

void FCSManagedReferencesCollection::ForEachManagedReference(const TFunction<void(UStruct*)>& Func)
{
	for (TWeakObjectPtr<UStruct> Struct : ManagedWeakReferences)
	{
		if (!Struct.IsValid())
		{
			continue;
		}

		Func(Struct.Get());
	}
}
