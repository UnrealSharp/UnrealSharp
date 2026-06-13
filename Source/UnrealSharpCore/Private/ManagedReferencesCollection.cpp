#include "ManagedReferencesCollection.h"
#include "Logging/StructuredLog.h"

#if WITH_EDITOR
void FCSReferencesCollection::AddReference(UStruct* Struct)
{
	if (References.Contains(Struct))
	{
		return;
	}

	References.Add(Struct);
}

void FCSReferencesCollection::RemoveReference(UStruct* Struct)
{
	References.Remove(Struct);
}
#endif
