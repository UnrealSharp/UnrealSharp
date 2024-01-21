#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "CSharpForUE/CSManager.h"
#include "ULocalPlayerExporter.generated.h"

UCLASS()
class CSHARPFORUE_API UULocalPlayerExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:
	
	static void* GetLocalPlayerSubsystem(UClass* SubsystemClass, APlayerController* PlayerController);
};
