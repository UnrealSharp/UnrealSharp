#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_UInputComponent)
{
	void BindAction(UInputComponent* InputComponent, const FName ActionName, const EInputEvent KeyEvent, UObject* Object, const FName FunctionName, bool bConsumeInput, bool bExecuteWhenPaused)
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

	void BindActionKeySignature(UInputComponent* InputComponent, const FName ActionName, const EInputEvent KeyEvent, UObject* Object, const FName FunctionName, bool bConsumeInput, bool bExecuteWhenPaused)
	{
		if (!IsValid(InputComponent))
		{
			return;
		}

		FInputActionHandlerDynamicSignature Handler;
		Handler.BindUFunction(Object, FunctionName);

		FInputActionBinding Binding(ActionName, KeyEvent);
		Binding.ActionDelegate = Handler;
		Binding.bConsumeInput = bConsumeInput;
		Binding.bExecuteWhenPaused = bExecuteWhenPaused;
		InputComponent->AddActionBinding(Binding);
	}

	void BindAxis(UInputComponent* InputComponent, const FName AxisName, UObject* Object, const FName FunctionName, bool bConsumeInput, bool bExecuteWhenPaused)
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
	
	BIND_UNREALSHARP_FUNCTION(BindAction)
	BIND_UNREALSHARP_FUNCTION(BindActionKeySignature)
	BIND_UNREALSHARP_FUNCTION(BindAxis)
}
