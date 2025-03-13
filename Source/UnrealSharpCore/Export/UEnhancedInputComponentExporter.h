#pragma once

#include "CoreMinimal.h"
#include "UnrealSharpBinds.h"
#include "UEnhancedInputComponentExporter.generated.h"

enum class ETriggerEvent : uint8;

class UInputAction;
class UEnhancedInputComponent;

UCLASS(meta = (NotGeneratorValid))
class UNREALSHARPCORE_API UUEnhancedInputComponentExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void BindAction(UEnhancedInputComponent* InputComponent, UInputAction* InputAction, ETriggerEvent TriggerEvent, UObject* Object, const FName FunctionName);
	
};
