// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "UCoreUObjectExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class UNREALSHARPCORE_API UUCoreUObjectExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctions interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static UClass* GetNativeClassFromName(const char* InAssemblyName, const char* InNamespace, const char* InClassName);
	static UScriptStruct* GetNativeStructFromName(const char* InAssemblyName, const char* InNamespace, const char* InStructName);
};
