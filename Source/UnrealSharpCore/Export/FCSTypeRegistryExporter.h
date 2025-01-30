// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FCSTypeRegistryExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class UFCSTypeRegistryExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static void RegisterClassToFilePath(const UTF16CHAR* ClassName, const UTF16CHAR* FilePath);
	
};
