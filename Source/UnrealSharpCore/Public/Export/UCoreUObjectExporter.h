#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UCoreUObjectExporter.generated.h"

UCLASS()
class UUCoreUObjectExporter : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARP_FUNCTION()
	static UField* GetType(const char* InAssemblyName, const char* InNamespace, const char* InTypeName);
	
	UNREALSHARP_FUNCTION()
	static UDelegateFunction* GetNativeDelegate(const char* PackageName, const char* OuterName, const char* DelegateName);
	
	UNREALSHARP_FUNCTION()
	static UField* GetGeneratedClassFromSkeleton(UField* InType);
};
