#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "UEnhancedInputComponentExporter.generated.h"

enum class ETriggerEvent : uint8;

class UInputAction;
class UEnhancedInputComponent;

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UUEnhancedInputComponentExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static void BindAction(UEnhancedInputComponent* InputComponent, UInputAction* InputAction, ETriggerEvent TriggerEvent, UObject* Object, const FName FunctionName);
	
};
