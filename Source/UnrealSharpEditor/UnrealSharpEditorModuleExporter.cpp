#include "UnrealSharpEditorModuleExporter.h"

#include "UnrealSharpProcHelper/CSProcHelper.h"

void UFUnrealSharpEditorModuleExporter::InitializeUnrealSharpEditorCallbacks(FManagedUnrealSharpEditorCallbacks Callbacks)
{
	FUnrealSharpEditorModule::Get().InitializeUnrealSharpEditorCallbacks(Callbacks);
}

void UFUnrealSharpEditorModuleExporter::GetProjectPaths(TArray<FString>* Paths)
{
	FCSProcHelper::GetAllProjectPaths(*Paths, true);
}
