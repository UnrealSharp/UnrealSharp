#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FQuatExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFQuatExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void ToQuaternion(FQuat* Quaternion, const FRotator& Rotator);

	UNREALSHARP_FUNCTION()
	static void ToRotator(FRotator* Rotator, const FQuat& Quaternion);
	
};
