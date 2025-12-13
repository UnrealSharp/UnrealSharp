#include "Export/UEnhancedInputComponentExporter.h"
#include "EnhancedInputComponent.h"

bool UUEnhancedInputComponentExporter::BindAction(UEnhancedInputComponent* InputComponent, UInputAction* InputAction, ETriggerEvent TriggerEvent, UObject* Object, const FName FunctionName, uint32* OutHandle)
{
	if (!IsValid(InputComponent) || !IsValid(InputAction))
	{
		return false;
	}
	*OutHandle = InputComponent->BindAction(InputAction, TriggerEvent, Object, FunctionName).GetHandle();
	return true;
}

bool UUEnhancedInputComponentExporter::RemoveBindingByHandle(UEnhancedInputComponent* InputComponent, const uint32 Handle)
{
	if (!IsValid(InputComponent))
	{
		return false;
	}

	return InputComponent->RemoveBindingByHandle(Handle);
}
