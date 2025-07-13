#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UScriptStructExporter.generated.h"

union FNativeStructData
{
    std::array<std::byte, 64> SmallStorage;
    void* LargeStorage;
};

UCLASS()
class UNREALSHARPCORE_API UUScriptStructExporter : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARP_FUNCTION()
	static int GetNativeStructSize(const UScriptStruct* ScriptStruct);

	UNREALSHARP_FUNCTION()
	static bool NativeCopy(const UScriptStruct* ScriptStruct, void* Src, void* Dest);
	
	UNREALSHARP_FUNCTION()
	static bool NativeDestroy(const UScriptStruct* ScriptStruct, void* Struct);

    UNREALSHARP_FUNCTION()
    static void AllocateNativeStruct(FNativeStructData& Data, const UScriptStruct* ScriptStruct);

    UNREALSHARP_FUNCTION()
    static void DeallocateNativeStruct(FNativeStructData& Data, const UScriptStruct* ScriptStruct);

    UNREALSHARP_FUNCTION()
    static void* GetStructLocation(FNativeStructData& Data, const UScriptStruct* ScriptStruct);
};
