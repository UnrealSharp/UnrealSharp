#include "UnrealSharpEditorModuleExporter.h"
#include "CSManager.h"
#include "CSProcUtilities.h"
#include "HotReload/CSHotReloadSubsystem.h"

void UFUnrealSharpEditorModuleExporter::InitializeUnrealSharpEditorCallbacks(FCSManagedEditorCallbacks Callbacks)
{
	FUnrealSharpEditorModule::Get().InitializeManagedEditorCallbacks(Callbacks);
}

void UFUnrealSharpEditorModuleExporter::GetProjectPaths(TArray<FString>* Paths)
{
	UCSProcUtilities::GetAllProjectPaths(*Paths, true);
}

void UFUnrealSharpEditorModuleExporter::DirtyUnrealType(const char* AssemblyName, const char* Namespace, const char* TypeName)
{
	UCSHotReloadSubsystem* HotReloadSubsystem = UCSHotReloadSubsystem::Get();
	
	if (!IsValid(HotReloadSubsystem))
	{
		UE_LOGFMT(LogUnrealSharpEditor, Warning, "Failed to dirty Unreal type. HotReloadSubsystem is not valid.");
		return;
	}
	
	HotReloadSubsystem->DirtyUnrealType(AssemblyName, Namespace, TypeName);
}
