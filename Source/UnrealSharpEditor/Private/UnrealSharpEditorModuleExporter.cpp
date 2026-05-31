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
	UnrealSharp::Project::GetAllProjectPaths(*Paths);
}

void UFUnrealSharpEditorModuleExporter::DirtyUnrealType(const char* AssemblyName, const char* Namespace, const char* TypeName, ECSTypeStructuralFlags Flags)
{
	UCSHotReloadSubsystem::Get()->DirtyUnrealType(AssemblyName, Namespace, TypeName, Flags);
}
