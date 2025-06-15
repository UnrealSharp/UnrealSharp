#pragma once

#include "CSTypeInfo.h"
#include "TypeGenerator/Register/MetaData/CSClassMetaData.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSGeneratedClassBuilder.h"

struct UNREALSHARPCORE_API FCSClassInfo : TCSTypeInfo<FCSClassMetaData, UClass, FCSGeneratedClassBuilder>
{
	FCSClassInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly);
	FCSClassInfo(UClass* InField, const TSharedPtr<FCSAssembly>& InOwningAssembly, const TSharedPtr<FGCHandle>& TypeHandle);

	// TCharpTypeInfo interface implementation
	virtual UClass* InitializeBuilder() override;
	// End of implementation

	TSharedPtr<FGCHandle> GetManagedTypeHandle();

private:
	friend struct FCSAssembly;
	TSharedPtr<FGCHandle> ManagedTypeHandle;
};
