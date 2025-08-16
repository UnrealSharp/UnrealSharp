#pragma once

#include "CSManagedTypeInfo.h"

struct UNREALSHARPCORE_API FCSClassInfo final : FCSManagedTypeInfo
{
	FCSClassInfo(const TSharedPtr<FCSTypeReferenceMetaData>& MetaData, UCSAssembly* InOwningAssembly, UClass* InClass)
		: FCSManagedTypeInfo(MetaData, InOwningAssembly, InClass)
	{
	}

	FCSClassInfo(UField* InField, UCSAssembly* InOwningAssembly)
		: FCSManagedTypeInfo(InField, InOwningAssembly)
	{
	}

	// FCSManagedTypeInfo interface implementation
	virtual UField* StartBuildingManagedType() override;
	// End of implementation
};
