// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FQuatExporter.generated.h"

/**
 * 
 */
UCLASS()
class CSHARPFORUE_API UFQuatExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctions interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static void ToQuaternion(FQuat& Quaternion, const FRotator& Rotator);
	
};
