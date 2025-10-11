#pragma once

#include "CSManagedTypeInfo.h"

struct UNREALSHARPCORE_API FCSClassInfo final : FCSManagedTypeInfo
{
	FCSClassInfo(const TSharedPtr<FCSTypeReferenceMetaData>& MetaData, UCSAssembly* InOwningAssembly)
		: FCSManagedTypeInfo(MetaData, InOwningAssembly)
	{
	}

	FCSClassInfo(UField* InField, UCSAssembly* InOwningAssembly)
		: FCSManagedTypeInfo(InField, InOwningAssembly)
	{
	}

	// FCSManagedTypeInfo interface implementation
	virtual void OnStructureChanged() override;
	virtual UField* StartBuildingType() override;
	// End of implementation
};
