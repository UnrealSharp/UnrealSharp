#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_UDataTable)
{
	uint8* GetRow(const UDataTable* DataTable, FName RowName)
	{
		if (!IsValid(DataTable))
		{
			return nullptr;
		}

		return DataTable->FindRowUnchecked(RowName);
	}
	
	BIND_UNREALSHARP_FUNCTION(GetRow)
}

