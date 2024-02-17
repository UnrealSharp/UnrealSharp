#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "UClassExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UUClassExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static UFunction* GetNativeFunctionFromClassAndName(const UClass* Class, const char* FunctionName);
	static UFunction* GetNativeFunctionFromInstanceAndName(const UObject* NativeObject, const char* FunctionName);
	static void* GetDefaultFromString(const char* ClassName);
	static void* GetDefaultFromInstance(UObject* Object);
};
