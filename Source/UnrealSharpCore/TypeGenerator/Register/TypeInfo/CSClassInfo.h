#pragma once

#include "CSTypeInfo.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSGeneratedClassBuilder.h"
#include "UObject/Class.h"

struct UNREALSHARPCORE_API FCSharpClassInfo : TCSharpTypeInfo<FCSClassMetaData, UClass, FCSGeneratedClassBuilder>
{
	FCSharpClassInfo(const TSharedPtr<FJsonValue>& MetaData);
	FCSharpClassInfo(UClass* InField);

	// TCharpTypeInfo interface implementation
	virtual UClass* InitializeBuilder() override;
	// End of implementation
	
	void TryUpdateTypeHandle();

	// Pointer to the TypeHandle in CSharp
	uint8* TypeHandle;

private:
	static uint8* GetHandle(UClass* Class);
	void OnAssembliesLoaded();
	
	bool bDirtyHandle = false;
	bool bInitFromClass = false;
};
