// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FWorldDelegatesExporter.generated.h"

using FWorldCleanupEventDelegate = void(*)(UWorld*, bool, bool);

UCLASS(meta = (NotGeneratorValid))
class UNREALSHARPCORE_API UFWorldDelegatesExporter : public UFunctionsExporter
{
	GENERATED_BODY()
public:
	
	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:
	static void BindOnWorldCleanup(FWorldCleanupEventDelegate Delegate, FDelegateHandle& Handle);
	static void UnbindOnWorldCleanup(FDelegateHandle Handle);
	
};
