#include "CSBindsRegistry.h"
#include "CSProjectUtilities.h"
#include "HotReload/CSHotReloadSubsystem.h"
#include "Logging/StructuredLog.h"

DECLARE_UNREALSHARP_BINDER(Bind_FUnrealSharpEditorModule)
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
	
	BIND_UNREALSHARP_FUNCTION(InitializeUnrealSharpEditorCallbacks)
	BIND_UNREALSHARP_FUNCTION(GetProjectPaths)
	BIND_UNREALSHARP_FUNCTION(DirtyUnrealType)
}
