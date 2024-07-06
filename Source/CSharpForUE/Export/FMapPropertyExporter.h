// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FMapPropertyExporter.generated.h"

UCLASS(meta=(NotGeneratorValid))
class CSHARPFORUE_API UFMapPropertyExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:
	
	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End of implementation

private:
	
	static void* GetKeyProperty(FMapProperty* MapProperty);
	static void* GetValueProperty(FMapProperty* MapProperty);
	static FScriptMapLayout GetScriptLayout(FMapProperty* MapProperty);
	
};
