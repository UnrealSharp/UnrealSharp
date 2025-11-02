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
	static UField* GetType(const char* InAssemblyName, const char* InNamespace, const char* InTypeName);
	
	UNREALSHARP_FUNCTION()
	static UField* GetGeneratedClassFromSkeleton(UField* InType);
};
