#pragma once

#include "CSTypeInfo.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSGeneratedClassBuilder.h"
#include "UObject/Class.h"

struct UNREALSHARPCORE_API FCSharpClassInfo : TCSharpTypeInfo<FCSClassMetaData, UClass, FCSGeneratedClassBuilder>
{
	FCSharpClassInfo(const TSharedPtr<FJsonValue>& MetaData) : TCSharpTypeInfo(MetaData)
	{
		TypeHandle = UCSManager::Get().GetTypeHandle(*TypeMetaData);
	}
	
	FCSharpClassInfo() {};

	// TCharpTypeInfo interface implementation
	virtual UClass* InitializeBuilder() override;
	// End of implementation
	
	// Pointer to the TypeHandle in CSharp
	uint8* TypeHandle;
};
