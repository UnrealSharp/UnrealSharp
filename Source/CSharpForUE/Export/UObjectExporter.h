// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "UObjectExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UUObjectExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static void* CreateNewObject(UObject* Outer, UClass* Class, UObject* Template);
	static void* GetTransientPackage();
	static FName NativeGetName(UObject* Object);
	static void InvokeNativeFunction(UObject* NativeObject, UFunction* NativeFunction, uint8* Params);
	static void InvokeNativeStaticFunction(const UClass* NativeClass, UFunction* NativeFunction, uint8* Params);
	static bool NativeIsValid(UObject* Object);
};
