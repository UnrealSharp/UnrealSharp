#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(UDataTableExporter)
{
	uint8* GetRow(const UDataTable* DataTable, FName RowName)
	{
		if (!IsValid(DataTable))
		{
			return nullptr;
		}

		return DataTable->FindRowUnchecked(RowName);
	}
	
	EXPORT_UNREALSHARP_FUNCTION(GetRow)
}

