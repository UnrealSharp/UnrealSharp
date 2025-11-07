#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UObject/Object.h"
#include "FOptionalPropertyExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFOptionalPropertyExporter : public UObject
{
	GENERATED_BODY()
	
public:

	UNREALSHARP_FUNCTION()
	static bool IsSet(const FOptionalProperty* OptionalProperty, const void* ScriptValue);

	UNREALSHARP_FUNCTION()
	static void* MarkSetAndGetInitializedValuePointerToReplace(const FOptionalProperty* OptionalProperty, void* Data);

	UNREALSHARP_FUNCTION()
	static void MarkUnset(const FOptionalProperty* OptionalProperty, void* Data);

	UNREALSHARP_FUNCTION()
	static const void* GetValuePointerForRead(const FOptionalProperty* OptionalProperty, const void* Data);

	UNREALSHARP_FUNCTION()
	static void* GetValuePointerForReplace(const FOptionalProperty* OptionalProperty, void* Data);

	UNREALSHARP_FUNCTION()
	static const void* GetValuePointerForReadIfSet(const FOptionalProperty* OptionalProperty, const void* Data);

	UNREALSHARP_FUNCTION()
	static void* GetValuePointerForReplaceIfSet(const FOptionalProperty* OptionalProperty, void* Data);
	
	UNREALSHARP_FUNCTION()
	static void* GetValuePointerForReadOrReplace(const FOptionalProperty* OptionalProperty, void* Data);

	UNREALSHARP_FUNCTION()
	static void* GetValuePointerForReadOrReplaceIfSet(const FOptionalProperty* OptionalProperty, void* Data);

	UNREALSHARP_FUNCTION()
	static int32 CalcSize(const FOptionalProperty* OptionalProperty);

    UNREALSHARP_FUNCTION()
    static void DestructInstance(const FOptionalProperty* OptionalProperty, void* Data);
};
