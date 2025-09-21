#include "CSObjectExtensions.h"

AWorldSettings* UCSObjectExtensions::GetWorldSettings(const UObject* Object)
{
	if (!IsValid(Object) || !Object->GetWorld())
	{
		return nullptr;
	}

	return Object->GetWorld()->GetWorldSettings();
}

void UCSObjectExtensions::MarkAsGarbage(UObject* Object)
{
	if (!IsValid(Object))
	{
		return;
	}

	Object->MarkAsGarbage();
}

bool UCSObjectExtensions::IsTemplate(const UObject* Object)
{
	if (!IsValid(Object))
	{
		return false;
	}

	return Object->IsTemplate();
}

UClass* UCSObjectExtensions::K2_GetClass(const UObject* Object)
{
	if (!IsValid(Object))
	{
		return nullptr;
	}

	return Object->GetClass();
}

UObject* UCSObjectExtensions::GetOuter(const UObject* Object)
{
	if (!IsValid(Object))
	{
		return nullptr;
	}

	return Object->GetOuter();
}

