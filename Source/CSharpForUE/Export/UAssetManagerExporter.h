// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "UAssetManagerExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UUAssetManagerExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctions interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static void* GetAssetManager();
	
};
