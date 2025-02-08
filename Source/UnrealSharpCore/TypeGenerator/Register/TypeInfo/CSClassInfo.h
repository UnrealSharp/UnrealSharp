#pragma once

#include "CSTypeInfo.h"
#include "TypeGenerator/Register/MetaData/CSClassMetaData.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSGeneratedClassBuilder.h"

struct UNREALSHARPCORE_API FCSharpClassInfo : TCSharpTypeInfo<FCSClassMetaData, UClass, FCSGeneratedClassBuilder>
{
	FCSharpClassInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly);
	FCSharpClassInfo(UClass* InField, const TSharedPtr<FCSAssembly>& InOwningAssembly, const TWeakPtr<FGCHandle>& TypeHandle);

	// TCharpTypeInfo interface implementation
	virtual UClass* InitializeBuilder() override;
	// End of implementation

	TWeakPtr<FGCHandle> GetTypeHandle() const { return TypeHandle; }

private:
	friend struct FCSAssembly;
	
	TWeakPtr<FGCHandle> TypeHandle;
	bool bDirtyHandle = false;
	bool bInitFromClass = false;
};
