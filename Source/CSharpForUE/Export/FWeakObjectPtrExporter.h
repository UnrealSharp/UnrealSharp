// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FWeakObjectPtrExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UFWeakObjectPtrExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static void SetObject(TWeakObjectPtr<UObject>& WeakObject, UObject* Object);
	static void* GetObject(TWeakObjectPtr<UObject> WeakObjectPtr);
	static bool IsValid(TWeakObjectPtr<UObject> WeakObjectPtr);
	static bool IsStale(TWeakObjectPtr<UObject> WeakObjectPtr);
	static bool NativeEquals(TWeakObjectPtr<UObject> A, TWeakObjectPtr<UObject> B);
};

