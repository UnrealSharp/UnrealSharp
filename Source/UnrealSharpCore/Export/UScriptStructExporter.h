﻿#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UScriptStructExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UUScriptStructExporter : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARP_FUNCTION()
	static int GetNativeStructSize(const UScriptStruct* ScriptStruct);

	UNREALSHARP_FUNCTION()
	static bool NativeCopy(const UScriptStruct* ScriptStruct, void* Src, void* Dest);
};
