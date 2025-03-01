#pragma once

#include "CSTypeInfo.h"
#include "TypeGenerator/Register/MetaData/CSClassMetaData.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSGeneratedClassBuilder.h"

struct UNREALSHARPCORE_API FCSharpClassInfo : TCSharpTypeInfo<FCSClassMetaData, UClass, FCSGeneratedClassBuilder>
{
	FCSharpClassInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly);
	FCSharpClassInfo(UClass* InField, const TSharedPtr<FCSAssembly>& InOwningAssembly, const TSharedPtr<FGCHandle>& TypeHandle);

	// TCharpTypeInfo interface implementation
	virtual UClass* InitializeBuilder() override;
	// End of implementation

	TSharedPtr<FGCHandle> GetTypeHandle();

private:
	friend struct FCSAssembly;
	TSharedPtr<FGCHandle> TypeHandle;
};
