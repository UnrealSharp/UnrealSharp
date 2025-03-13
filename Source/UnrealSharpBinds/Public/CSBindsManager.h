#pragma once

#include "CoreMinimal.h"
#include "UnrealSharpBinds.h"
#include "UObject/Object.h"
#include "CSBindsManager.generated.h"

UCLASS()
class UCSBindsManager : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARPBINDS_API static UCSBindsManager* Get();

	UNREALSHARPBINDS_API static void RegisterExportedFunction(const FName& ClassName, const FCSExportedFunction& ExportedFunction);
	UNREALSHARPBINDS_API static void* GetBoundNativeFunction(TCHAR* OuterName, TCHAR* FunctionName, int32 ManagedFunctionSize);
	
private:
	static UCSBindsManager* BindsManagerInstance;
	TMap<FName, TArray<FCSExportedFunction>> ExportedFunctionsMap;
};
