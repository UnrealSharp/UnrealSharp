#pragma once

#include "CoreMinimal.h"
#include "UnrealSharpBinds.h"
#include "UDataTableExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UUDataTableExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static uint8* GetRow(const UDataTable* DataTable, FName RowName);
	
};
