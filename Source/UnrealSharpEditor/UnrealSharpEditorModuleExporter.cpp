#include "UnrealSharpEditorModuleExporter.h"

#include "UnrealSharpProcHelper/CSProcHelper.h"

void UFUnrealSharpEditorModuleExporter::InitializeUnrealSharpEditorCallbacks(FCSManagedUnrealSharpEditorCallbacks Callbacks)
{
	FUnrealSharpEditorModule::Get().InitializeUnrealSharpEditorCallbacks(Callbacks);
}

void UFUnrealSharpEditorModuleExporter::GetProjectPaths(TArray<FString>* Paths)
{
	FCSProcHelper::GetAllProjectPaths(*Paths, true);
}
