#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSEditorBlueprintFunctionLibrary.generated.h"

UCLASS()
class UCSEditorBlueprintFunctionLibrary : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
public:
	UFUNCTION(meta = (ScriptMethod))
	static void SetupAssemblyReferences(FName AssemblyName, const TArray<FName>& DependentAssemblyNames, const TArray<FName>& ReferencedAssemblyNames);
};
