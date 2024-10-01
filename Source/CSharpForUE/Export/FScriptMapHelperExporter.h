#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FScriptMapHelperExporter.generated.h"

UCLASS(meta=(NotGeneratorValid))
class CSHARPFORUE_API UFScriptMapHelperExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End of implementation

private:
	
	static void AddPair(FMapProperty* MapProperty, const void* Address, const void* Key, const void* Value);
	static void* FindOrAdd(FMapProperty* MapProperty, const void* Address, const void* Key);
	static int Num(FMapProperty* MapProperty, const void* Address);
	static int FindMapPairIndexFromHash(FMapProperty* MapProperty, const void* Address, const void* Key);
	static void RemoveIndex(FMapProperty* MapProperty, const void* Address, int Index);
	static void EmptyValues(FMapProperty* MapProperty, const void* Address);
	static void Remove(FMapProperty* MapProperty, const void* Address, const void* Key);
	static bool IsValidIndex(FMapProperty* MapProperty, const void* Address, int Index);
	static int GetMaxIndex(FMapProperty* MapProperty, const void* Address);
	static void* GetPairPtr(FMapProperty* MapProperty, const void* Address, int Index);
	
};
