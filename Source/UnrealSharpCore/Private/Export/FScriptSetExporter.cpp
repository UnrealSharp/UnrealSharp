#include "Export/FScriptSetExporter.h"

bool UFScriptSetExporter::IsValidIndex(FScriptSet* ScriptSet, int32 Index)
{
	return ScriptSet->IsValidIndex(Index);
}

int UFScriptSetExporter::Num(FScriptSet* ScriptSet)
{
	int Num = ScriptSet->Num();
	return Num;
}

int UFScriptSetExporter::GetMaxIndex(FScriptSet* ScriptSet)
{
	return ScriptSet->GetMaxIndex();
}

void* UFScriptSetExporter::GetData(int Index, FScriptSet* ScriptSet, FSetProperty* Property)
{
	return ScriptSet->GetData(Index, Property->SetLayout);
}

void UFScriptSetExporter::Empty(int Slack, FScriptSet* ScriptSet, FSetProperty* Property)
{
	return ScriptSet->Empty(Slack, Property->SetLayout);
}

void UFScriptSetExporter::RemoveAt(int Index, FScriptSet* ScriptSet, FSetProperty* Property)
{
	return ScriptSet->RemoveAt(Index, Property->SetLayout);
}

int UFScriptSetExporter::AddUninitialized(FScriptSet* ScriptSet, FSetProperty* Property)
{
	return ScriptSet->AddUninitialized(Property->SetLayout);
}

void UFScriptSetExporter::Add(FScriptSet* ScriptSet, FSetProperty* Property, const void* Element,FGetKeyHash GetKeyHash, FEqualityFn EqualityFn, FConstructFn ConstructFn, FDestructFn DestructFn)
{
	ScriptSet->Add(Element, Property->SetLayout, GetKeyHash, EqualityFn, ConstructFn, DestructFn);
}

int32 UFScriptSetExporter::FindOrAdd(FScriptSet* ScriptSet, FSetProperty* Property, const void* Element, FGetKeyHash GetKeyHash, FEqualityFn EqualityFn, FConstructFn ConstructFn)
{
	return ScriptSet->FindOrAdd(Element, Property->SetLayout, GetKeyHash, EqualityFn, ConstructFn);
}

int UFScriptSetExporter::FindIndex(FScriptSet* ScriptSet, FSetProperty* Property, const void* Element, FGetKeyHash GetKeyHash, FEqualityFn EqualityFn)
{
	return ScriptSet->FindIndex(Element, Property->SetLayout, GetKeyHash, EqualityFn);
}