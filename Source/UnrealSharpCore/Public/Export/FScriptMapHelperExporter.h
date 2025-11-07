#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FScriptMapHelperExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFScriptMapHelperExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void AddPair(FMapProperty* MapProperty, const void* Address, const void* Key, const void* Value);
	
	UNREALSHARP_FUNCTION()
	static void* FindOrAdd(FMapProperty* MapProperty, const void* Address, const void* Key);

	UNREALSHARP_FUNCTION()
	static int Num(FMapProperty* MapProperty, const void* Address);

	UNREALSHARP_FUNCTION()
	static int FindMapPairIndexFromHash(FMapProperty* MapProperty, const void* Address, const void* Key);

	UNREALSHARP_FUNCTION()
	static void RemoveIndex(FMapProperty* MapProperty, const void* Address, int Index);

	UNREALSHARP_FUNCTION()
	static void EmptyValues(FMapProperty* MapProperty, const void* Address);

	UNREALSHARP_FUNCTION()
	static void Remove(FMapProperty* MapProperty, const void* Address, const void* Key);

	UNREALSHARP_FUNCTION()
	static bool IsValidIndex(FMapProperty* MapProperty, const void* Address, int Index);

	UNREALSHARP_FUNCTION()
	static int GetMaxIndex(FMapProperty* MapProperty, const void* Address);

	UNREALSHARP_FUNCTION()
	static void* GetPairPtr(FMapProperty* MapProperty, const void* Address, int Index);
	
};
