#include "CSBindsManager.h"
#include "CSProjectUtilities.h"
#include "HotReload/CSHotReloadSubsystem.h"
#include "Logging/StructuredLog.h"

DECLARE_UNREALSHARP_EXPORTER(FUnrealSharpEditorModuleExporter)
{
	void InitializeUnrealSharpEditorCallbacks(FCSManagedEditorCallbacks Callbacks)
	{
		FUnrealSharpEditorModule::Get().InitializeManagedEditorCallbacks(Callbacks);
	}

	void GetProjectPaths(TArray<FString>* Paths)
	{
		UnrealSharp::Project::GetAllProjectPaths(*Paths);
	}

	void DirtyUnrealType(const char* AssemblyName, const char* Namespace, const char* TypeName, ECSTypeStructuralFlags Flags)
	{
		UCSHotReloadSubsystem::Get()->DirtyUnrealType(AssemblyName, Namespace, TypeName, Flags);
	}
	
	EXPORT_UNREALSHARP_FUNCTION(InitializeUnrealSharpEditorCallbacks)
	EXPORT_UNREALSHARP_FUNCTION(GetProjectPaths)
	EXPORT_UNREALSHARP_FUNCTION(DirtyUnrealType)
}
