#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UInputComponentExporter.generated.h"

class UInputAction;

UCLASS()
class UNREALSHARPCORE_API UUInputComponentExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void BindAction(UInputComponent* InputComponent, const FName ActionName, const EInputEvent KeyEvent, UObject* Object, const FName FunctionName, bool bConsumeInput, bool bExecuteWhenPaused);

	UNREALSHARP_FUNCTION()
	static void BindActionKeySignature(UInputComponent* InputComponent, const FName ActionName, const EInputEvent KeyEvent, UObject* Object, const FName FunctionName, bool bConsumeInput, bool bExecuteWhenPaused);

	UNREALSHARP_FUNCTION()
	static void BindAxis(UInputComponent* InputComponent, const FName AxisName, UObject* Object, const FName FunctionName, bool bConsumeInput, bool bExecuteWhenPaused);
	
};
