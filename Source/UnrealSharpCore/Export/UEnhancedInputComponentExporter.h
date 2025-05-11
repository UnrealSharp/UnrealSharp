#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UEnhancedInputComponentExporter.generated.h"

enum class ETriggerEvent : uint8;

class UInputAction;
class UEnhancedInputComponent;

UCLASS()
class UNREALSHARPCORE_API UUEnhancedInputComponentExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static bool BindAction(UEnhancedInputComponent* InputComponent, UInputAction* InputAction, ETriggerEvent TriggerEvent, UObject* Object, const FName FunctionName, uint32* OutHandle);

	UNREALSHARP_FUNCTION()
	static bool RemoveBindingByHandle(UEnhancedInputComponent* InputComponent, const uint32 Handle);
	
};
