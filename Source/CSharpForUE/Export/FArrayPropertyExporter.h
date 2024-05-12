// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FArrayPropertyExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UFArrayPropertyExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctions interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:
	
	static void InitializeArray(FArrayProperty* ArrayProperty, const void* ScriptArray, int Length);
	static void EmptyArray(FArrayProperty* ArrayProperty, const void* ScriptArray);
	static void AddToArray(FArrayProperty* ArrayProperty, const void* ScriptArray);
	static void InsertInArray(FArrayProperty* ArrayProperty, const void* ScriptArray, int index);
	static void RemoveFromArray(FArrayProperty* ArrayProperty, const void* ScriptArray, int index);
	static void ResizeArray(FArrayProperty* ArrayProperty, const void* ScriptArray, int Length);
	static void SwapValues(FArrayProperty* ArrayProperty, const void* ScriptArray, int indexA, int indexB);
	
};
