#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSPrimitiveComponentExtensions.generated.h"

class UPrimitiveComponent;

UCLASS(meta = (InternalType))
class UCSPrimitiveComponentExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()

public:

	/** The custom primitive data the component's materials read, as set with the SetCustomPrimitiveData* functions. */
	UFUNCTION(meta = (ExtensionMethod, ScriptMethod))
	static TArray<float> GetCustomPrimitiveData(const UPrimitiveComponent* PrimitiveComponent);
};
