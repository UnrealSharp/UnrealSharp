#pragma once

#include "CoreMinimal.h"
#include "CSExportedFunction.h"
#include "UObject/Object.h"
#include "CSBindsManager.generated.h"

#define UNREALSHARP_FUNCTION()

UCLASS()
class UCSBindsManager : public UObject
{
	GENERATED_BODY()
public:
	
	static UCSBindsManager* Get();
	
	UNREALSHARPBINDS_API static void RegisterExportedFunction(const FName& ClassName, const FCSExportedFunction& ExportedFunction);
	UNREALSHARPBINDS_API static void* GetBoundFunction(TCHAR* OuterName, TCHAR* FunctionName, int32 ManagedFunctionSize);
	
private:
	static UCSBindsManager* BindsManagerInstance;
	TMap<FName, TArray<FCSExportedFunction>> ExportedFunctionsMap;
};
