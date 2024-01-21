#pragma once

#include "Kismet2/ReloadUtilities.h"

class FCSReinstancer;
class UCSScriptStruct;
class UUserDefinedStruct;

class FCSReload : public FReload
{
public:

	FCSReload(EActiveReloadType InType, const TCHAR* InPrefix, FOutputDevice& InAr) : FReload(InType, InPrefix, InAr)
	{
	}

	void StartReinstancing(FCSReinstancer& Reinstancer);
	void FixDataTables(TArray<TPair<TObjectPtr<UScriptStruct>, TObjectPtr<UScriptStruct>>>& StructsToReinstance);

	static TArray<UDataTable*> GetTablesDependentOnStruct(UScriptStruct* Struct);
	
};
