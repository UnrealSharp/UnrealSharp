#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSDataTableExtensions.generated.h"

UCLASS(meta = (Internal))
class UNREALSHARPCORE_API UCSDataTableExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
public:
	UFUNCTION(meta=(ScriptMethod))
	static FString GetTableAsJSON(const UDataTable* DataTable);

	UFUNCTION(meta=(ScriptMethod))
	static FString GetTableAsCSV(const UDataTable* DataTable);
};

