#pragma once

#include "CSManagedTypeInfo.h"

struct UNREALSHARPCORE_API FCSClassInfo final : FCSManagedTypeInfo
{
	FCSClassInfo(const TSharedPtr<FCSTypeReferenceMetaData>& MetaData, UCSAssembly* InOwningAssembly, UClass* InClass)
		: FCSManagedTypeInfo(MetaData, InOwningAssembly, InClass)
	{
	}

	FCSClassInfo(UField* InField, UCSAssembly* InOwningAssembly, const TSharedPtr<FGCHandle>& TypeHandle)
		: FCSManagedTypeInfo(InField, InOwningAssembly, TypeHandle)
	{
	}

	// FCSManagedTypeInfo interface implementation
	virtual UField* InitializeBuilder() override;
	// End of implementation
};
