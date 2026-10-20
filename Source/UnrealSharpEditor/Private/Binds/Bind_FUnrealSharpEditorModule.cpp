#include "CSBindsRegistry.h"
#include "CSProjectUtilities.h"
#include "HotReload/CSHotReloadSubsystem.h"
#include "Logging/StructuredLog.h"
#include "Types/CSManagedTypeInterface.h"

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

	void DirtyUnrealType(UField* Field, ECSTypeStructuralFlags Flags)
	{
		ICSManagedTypeInterface* ManagedTypeInterface = Cast<ICSManagedTypeInterface>(Field);
		ManagedTypeInterface->GetManagedTypeDefinition()->SetDirtyFlags(Flags);
	}
	
	void NotifyNewType()
	{
		UCSHotReloadSubsystem::Get()->NotifyNewType();
	}
	
	BIND_UNREALSHARP_FUNCTION(InitializeUnrealSharpEditorCallbacks)
	BIND_UNREALSHARP_FUNCTION(GetProjectPaths)
	BIND_UNREALSHARP_FUNCTION(DirtyUnrealType)
	BIND_UNREALSHARP_FUNCTION(NotifyNewType)
}
