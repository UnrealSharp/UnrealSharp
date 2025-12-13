#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FStringExporter.generated.h"

UCLASS()
class UFStringExporter : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARP_FUNCTION()
	static void MarshalToNativeString(FString* NativeString, const char* ManagedString)
	{
		*NativeString = ManagedString;
	}
};
