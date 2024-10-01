#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "UInputComponentExporter.generated.h"

class UInputAction;

UCLASS(meta=(NotGeneratorValid))
class CSHARPFORUE_API UUInputComponentExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:
	
	// UFunctions interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End
	
private:

	static void BindAction(UInputComponent* InputComponent, const FName ActionName, const EInputEvent KeyEvent, UObject* Object, const FName FunctionName, bool bConsumeInput, bool bExecuteWhenPaused);
	static void BindActionKeySignature(UInputComponent* InputComponent, const FName ActionName, const EInputEvent KeyEvent, UObject* Object, const FName FunctionName, bool bConsumeInput, bool bExecuteWhenPaused);
	static void BindAxis(UInputComponent* InputComponent, const FName AxisName, UObject* Object, const FName FunctionName, bool bConsumeInput, bool bExecuteWhenPaused);
	
};
