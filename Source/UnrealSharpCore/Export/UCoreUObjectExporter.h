#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UCoreUObjectExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UUCoreUObjectExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static UClass* GetNativeClassFromName(const char* InAssemblyName, const char* InNamespace, const char* InClassName);

	UNREALSHARP_FUNCTION()
	static UScriptStruct* GetNativeStructFromName(const char* InAssemblyName, const char* InNamespace, const char* InStructName);
};
