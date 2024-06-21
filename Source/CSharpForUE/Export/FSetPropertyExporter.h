// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FSetPropertyExporter.generated.h"

/**
 * 
 */
UCLASS()
class CSHARPFORUE_API UFSetPropertyExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End of UFunctionsExporter interface

private:

	static void GetScriptSetLayout(FSetProperty* SetProperty, FScriptSetLayout* OutLayout);
	
};
