#include "CSReload.h"
#include "CSReinstancer.h"

void FCSReload::StartReinstancing(FCSReinstancer& Reinstancer)
{
	FixDataTables(Reinstancer.StructsToReinstance);
	Reinstance();
	Finalize();
}

TArray<UDataTable*> FCSReload::GetTablesDependentOnStruct(UScriptStruct* Struct)
{
	TArray<UDataTable*> Result;
	if (Struct)
	{
		TArray<UObject*> DataTables;
		GetObjectsOfClass(UDataTable::StaticClass(), DataTables);
		for (UObject* DataTableObj : DataTables)
		{
			UDataTable* DataTable = Cast<UDataTable>(DataTableObj);
			if (DataTable && (Struct == DataTable->RowStruct))
			{
				Result.Add(DataTable);
			}
		}
	}
	return Result;
}

void FCSReload::FixDataTables(TArray<TPair<TObjectPtr<UScriptStruct>, TObjectPtr<UScriptStruct>>>& StructsToReinstance)
{
	for (auto StructToReinstance : StructsToReinstance)
	{
		TArray<UDataTable*> DataTables = GetTablesDependentOnStruct(StructToReinstance.Key);
		
		for (UDataTable* DataTable : DataTables)
		{
			DataTable->CleanBeforeStructChange();
			DataTable->RowStruct = StructToReinstance.Value;
			DataTable->RestoreAfterStructChange();
		}
	}
}
