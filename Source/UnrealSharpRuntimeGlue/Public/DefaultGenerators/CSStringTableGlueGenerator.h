#pragma once

#include "CoreMinimal.h"
#include "CSGlueGenerator.h"
#include "UObject/ObjectSaveContext.h"
#include "CSStringTableGlueGenerator.generated.h"

UCLASS(DisplayName="String Table Glue Generator", NotBlueprintable, NotBlueprintType)
class UCSStringTableGlueGenerator : public UCSGlueGenerator
{
	GENERATED_BODY()
private:
	// UCSGlueGenerator interface
	virtual void Initialize() override;
	virtual void ForceRefresh() override { ProcessStringTables(); }
	// End of UCSGlueGenerator interface

	void OnPackageSaved(const FString& PackageFileName, UPackage* Package, FObjectPostSaveContext SaveContext);
	void ProcessStringTables();
};
