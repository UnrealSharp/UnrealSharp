#include "Export/UDataTableExporter.h"

uint8* UUDataTableExporter::GetRow(const UDataTable* DataTable, FName RowName)
{
	if (!IsValid(DataTable))
	{
		return nullptr;
	}

	return DataTable->FindRowUnchecked(RowName);
}
