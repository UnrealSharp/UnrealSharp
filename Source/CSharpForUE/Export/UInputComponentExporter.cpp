#include "UInputComponentExporter.h"

#include "../../../../../../../../../../Program Files/Epic Games/UE_5.3/Engine/Plugins/EnhancedInput/Source/EnhancedInput/Public/EnhancedInputComponent.h"
#include "boost/spirit/home/x3/numeric/uint.hpp"
#include "Engine/InputAxisDelegateBinding.h"

void UUInputComponentExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(BindAction)
	EXPORT_FUNCTION(BindAxis)
}

void UUInputComponentExporter::BindAction(UInputComponent* InputComponent, const FName ActionName, const EInputEvent KeyEvent, UObject* Object, const FName FunctionName, bool bConsumeInput, bool bExecuteWhenPaused)
{
	if (!IsValid(InputComponent))
	{
		return;
	}

	FInputActionHandlerSignature Handler;
	Handler.BindUFunction(Object, FunctionName);

	FInputActionBinding Binding(ActionName, KeyEvent);
	Binding.ActionDelegate = Handler;
	Binding.bConsumeInput = bConsumeInput;
	Binding.bExecuteWhenPaused = bExecuteWhenPaused;
	
	InputComponent->AddActionBinding(Binding);
}

void UUInputComponentExporter::BindAxis(UInputComponent* InputComponent, const FName AxisName, UObject* Object, const FName FunctionName, bool bConsumeInput, bool bExecuteWhenPaused)
{
	if (!IsValid(InputComponent))
	{
		return;
	}

	FInputAxisBinding NewAxisBinding(AxisName);
	NewAxisBinding.bConsumeInput = bConsumeInput;
	NewAxisBinding.bExecuteWhenPaused = bExecuteWhenPaused;
	NewAxisBinding.AxisDelegate.BindDelegate(Object, FunctionName);
	InputComponent->AxisBindings.Add(NewAxisBinding);
}
