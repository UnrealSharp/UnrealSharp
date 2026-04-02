#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UDataTableExporter.generated.h"

UCLASS()
class UUDataTableExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static uint8* GetRow(const UDataTable* DataTable, FName RowName);
	
};
