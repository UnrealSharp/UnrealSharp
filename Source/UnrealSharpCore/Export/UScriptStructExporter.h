#pragma once

#include "CoreMinimal.h"
#include "UnrealSharpBinds.h"
#include "UScriptStructExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class UNREALSHARPCORE_API UUScriptStructExporter : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARP_FUNCTION()
	static int GetNativeStructSize(const UScriptStruct* ScriptStruct);
};
