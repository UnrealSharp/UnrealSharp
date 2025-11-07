#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FStringExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFStringExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void MarshalToNativeString(FString* String, TCHAR* ManagedString);
	
};
