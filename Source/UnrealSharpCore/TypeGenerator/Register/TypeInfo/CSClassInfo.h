#pragma once

#include "CSTypeInfo.h"
#include "TypeGenerator/Register/MetaData/CSClassMetaData.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSGeneratedClassBuilder.h"

struct UNREALSHARPCORE_API FCSharpClassInfo : TCSharpTypeInfo<FCSClassMetaData, UClass, FCSGeneratedClassBuilder>
{
	FCSharpClassInfo(const TSharedPtr<FJsonValue>& MetaData,const TSharedPtr<FCSAssembly>& InOwningAssembly);
	FCSharpClassInfo(UClass* InField);

	// TCharpTypeInfo interface implementation
	virtual UClass* InitializeBuilder() override;
	// End of implementation
	
	void TryUpdateTypeHandle();

	TSharedPtr<FGCHandle> TypeHandle;

private:
	void OnNewClassOrModified(UClass* Class);
	uint8* GetHandle(UClass* Class);
	void OnAssembliesLoaded();
	
	bool bDirtyHandle = false;
	bool bInitFromClass = false;
};
