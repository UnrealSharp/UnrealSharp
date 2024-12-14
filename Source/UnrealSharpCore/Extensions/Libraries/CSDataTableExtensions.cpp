#include "CSDataTableExtensions.h"

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
