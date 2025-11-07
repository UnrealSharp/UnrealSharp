#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FWeakObjectPtrExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFWeakObjectPtrExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void SetObject(TWeakObjectPtr<UObject>& WeakObject, UObject* Object);

	UNREALSHARP_FUNCTION()
	static void* GetObject(TWeakObjectPtr<UObject> WeakObjectPtr);

	UNREALSHARP_FUNCTION()
	static bool IsValid(TWeakObjectPtr<UObject> WeakObjectPtr);

	UNREALSHARP_FUNCTION()
	static bool IsStale(TWeakObjectPtr<UObject> WeakObjectPtr);

	UNREALSHARP_FUNCTION()
	static bool NativeEquals(TWeakObjectPtr<UObject> A, TWeakObjectPtr<UObject> B);
};

