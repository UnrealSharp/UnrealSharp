#include "UDataTableExporter.h"

void UUDataTableExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetRow)
}

uint8* UUDataTableExporter::GetRow(const UDataTable* DataTable, FName RowName)
{
	if (!IsValid(DataTable))
	{
		return nullptr;
	}

	return DataTable->FindRowUnchecked(RowName);
}
