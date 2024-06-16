#include "FScriptSetExporter.h"

void UFScriptSetExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(IsValidIndex);
	EXPORT_FUNCTION(Num);
	EXPORT_FUNCTION(GetMaxIndex);
	EXPORT_FUNCTION(GetData);
	EXPORT_FUNCTION(Empty);
	EXPORT_FUNCTION(RemoveAt);
	EXPORT_FUNCTION(AddUninitialized);
	EXPORT_FUNCTION(GetScriptSetLayout);
}

bool UFScriptSetExporter::IsValidIndex(FScriptSet* ScriptSet, int32 Index)
{
	return ScriptSet->IsValidIndex(Index);
}

int UFScriptSetExporter::Num(FScriptSet* ScriptSet)
{
	return ScriptSet->Num();
}

int UFScriptSetExporter::GetMaxIndex(FScriptSet* ScriptSet)
{
	return ScriptSet->GetMaxIndex();
}

void* UFScriptSetExporter::GetData(int Index, FScriptSet* ScriptSet, FScriptSetLayout* Layout)
{
	return ScriptSet->GetData(Index, *Layout);
}

void UFScriptSetExporter::Empty(int Slack, FScriptSet* ScriptSet, FScriptSetLayout* Layout)
{
	return ScriptSet->Empty(Slack, *Layout);
}

void UFScriptSetExporter::RemoveAt(int Index, FScriptSet* ScriptSet, FScriptSetLayout* Layout)
{
	return ScriptSet->RemoveAt(Index, *Layout);
}

int UFScriptSetExporter::AddUninitialized(FScriptSet* ScriptSet, FScriptSetLayout* Layout)
{
	return ScriptSet->AddUninitialized(*Layout);
}

FScriptSetLayout UFScriptSetExporter::GetScriptSetLayout(int elementSize, int elementAlignment)
{
	return FScriptSetLayout(elementSize, elementAlignment);
}
