#include "UEnhancedInputComponentExporter.h"
#include "EnhancedInputComponent.h"

void UUEnhancedInputComponentExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(BindAction)
}

void UUEnhancedInputComponentExporter::BindAction(UEnhancedInputComponent* InputComponent, UInputAction* InputAction, ETriggerEvent TriggerEvent, UObject* Object, const FName FunctionName)
{
	if (!IsValid(InputComponent) || !IsValid(InputAction))
	{
		return;
	}
	
	InputComponent->BindAction(InputAction, TriggerEvent, Object, FunctionName);
}
