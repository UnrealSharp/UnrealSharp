#pragma once

#include "CSManagedTypeInfo.h"

struct UNREALSHARPCORE_API FCSClassInfo : FCSManagedTypeInfo
{
	FCSClassInfo(const TSharedPtr<FCSTypeReferenceMetaData>& MetaData, UCSAssembly* InOwningAssembly, UClass* InClass);
	FCSClassInfo(UClass* InField, UCSAssembly* InOwningAssembly, const TSharedPtr<FGCHandle>& TypeHandle);

	// FCSManagedTypeInfo interface implementation
	virtual UField* InitializeBuilder() override;
	// End of implementation
};
