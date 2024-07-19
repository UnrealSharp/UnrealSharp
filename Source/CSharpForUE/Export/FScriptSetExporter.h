#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FScriptSetExporter.generated.h"

UCLASS(meta=(NotGeneratorValid))
class CSHARPFORUE_API UFScriptSetExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End of UFunctionsExporter interface

private:

	static bool IsValidIndex(FScriptSet* ScriptSet, int32 Index);
	static int Num(FScriptSet* ScriptSet);
	static int GetMaxIndex(FScriptSet* ScriptSet);
	static void* GetData(int Index, FScriptSet* ScriptSet, FScriptSetLayout* Layout);
	static void Empty(int Slack, FScriptSet* ScriptSet, FScriptSetLayout* Layout);
	static void RemoveAt(int Index, FScriptSet* ScriptSet, FScriptSetLayout* Layout);
	static int AddUninitialized(FScriptSet* ScriptSet, FScriptSetLayout* Layout);
	static FScriptSetLayout GetScriptSetLayout(int elementSize, int elementAlignment);
	
	
};
