#include "UnrealSharpEditorModuleExporter.h"
#include "CSProjectUtilities.h"
#include "HotReload/CSHotReloadSubsystem.h"
#include "Logging/StructuredLog.h"

void UFUnrealSharpEditorModuleExporter::InitializeUnrealSharpEditorCallbacks(FCSManagedEditorCallbacks Callbacks)
{
	FUnrealSharpEditorModule::Get().InitializeManagedEditorCallbacks(Callbacks);
}

void UFUnrealSharpEditorModuleExporter::GetProjectPaths(TArray<FString>* Paths)
{
	UnrealSharp::Project::GetAllProjectPaths(*Paths, true);
}

void UFUnrealSharpEditorModuleExporter::DirtyUnrealType(const char* AssemblyName, const char* Namespace, const char* TypeName, ECSTypeStructuralFlags Flags)
{
	UCSHotReloadSubsystem* HotReloadSubsystem = UCSHotReloadSubsystem::Get();
	
	if (!IsValid(HotReloadSubsystem))
	{
		UE_LOGFMT(LogUnrealSharpEditor, Warning, "Failed to dirty Unreal type. HotReloadSubsystem is not valid.");
		return;
	}
	
	HotReloadSubsystem->DirtyUnrealType(AssemblyName, Namespace, TypeName, Flags);
}
