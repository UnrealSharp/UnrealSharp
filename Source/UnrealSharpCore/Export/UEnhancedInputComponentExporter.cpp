#include "UEnhancedInputComponentExporter.h"
#include "EnhancedInputComponent.h"

void UUEnhancedInputComponentExporter::BindAction(UEnhancedInputComponent* InputComponent, UInputAction* InputAction, ETriggerEvent TriggerEvent, UObject* Object, const FName FunctionName)
{
	if (!IsValid(InputComponent) || !IsValid(InputAction))
	{
		return;
	}
	
	InputComponent->BindAction(InputAction, TriggerEvent, Object, FunctionName);
}
