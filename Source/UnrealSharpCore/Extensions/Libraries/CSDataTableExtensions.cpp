#include "CSDataTableExtensions.h"

#if WITH_EDITOR
FString UCSDataTableExtensions::GetTableAsJSON(const UDataTable* DataTable)
{
	if (!IsValid(DataTable))
	{
		return FString();
	}

	return DataTable->GetTableAsJSON();
}

FString UCSDataTableExtensions::GetTableAsCSV(const UDataTable* DataTable)
{
	if (!IsValid(DataTable))
	{
		return FString();
	}
	
	return DataTable->GetTableAsCSV();
}
#endif
