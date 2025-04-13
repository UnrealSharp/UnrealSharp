#include "UnrealSharpCore.h"
#include "CoreMinimal.h"
#include "CSManager.h"
#include "Modules/ModuleManager.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpCoreModule"

DEFINE_LOG_CATEGORY(LogUnrealSharp);

void FUnrealSharpCoreModule::StartupModule()
{
	// Initialize the C# runtime
	UCSManager& CSManager = UCSManager::GetOrCreate();
	CSManager.Initialize();
}

void FUnrealSharpCoreModule::ShutdownModule()
{
	UCSManager::Shutdown();
}

#undef LOCTEXT_NAMESPACE
	
IMPLEMENT_MODULE(FUnrealSharpCoreModule, UnrealSharpCore)