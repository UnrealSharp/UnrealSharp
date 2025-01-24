#include "UnrealSharpCompiler.h"

#include "BlueprintCompilationManager.h"
#include "CSBlueprintCompiler.h"
#include "CSCompilerContext.h"
#include "KismetCompiler.h"
#include "TypeGenerator/CSBlueprint.h"
#include "TypeGenerator/Register/CSTypeRegistry.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpCompilerModule"

void FUnrealSharpCompilerModule::StartupModule()
{
	FKismetCompilerContext::RegisterCompilerForBP(UCSBlueprint::StaticClass(), [](UBlueprint* InBlueprint, FCompilerResultsLog& InMessageLog, const FKismetCompilerOptions& InCompileOptions)
	{
		return MakeShared<FCSCompilerContext>(CastChecked<UCSBlueprint>(InBlueprint), InMessageLog, InCompileOptions);
	});
	
	IKismetCompilerInterface& KismetCompilerModule = FModuleManager::LoadModuleChecked<IKismetCompilerInterface>("KismetCompiler");
	KismetCompilerModule.GetCompilers().Add(&BlueprintCompiler);

	FCSTypeRegistry::Get().GetOnNewClassEvent().AddStatic(&FUnrealSharpCompilerModule::OnNewClass);
	UCSManager::GetOrCreate().OnManagedAssemblyLoadedEvent().AddStatic(&FUnrealSharpCompilerModule::OnManagedAssemblyLoaded);

	FBlueprintCompilationManager::FlushCompilationQueueAndReinstance();
}

void FUnrealSharpCompilerModule::ShutdownModule()
{
    
}

void FUnrealSharpCompilerModule::OnNewClass(UClass* OldClass, UClass* NewClass)
{
	if (UBlueprint* Blueprint = Cast<UBlueprint>(NewClass->ClassGeneratedBy))
	{
		FBlueprintCompilationManager::QueueForCompilation(Blueprint);
	}
}

void FUnrealSharpCompilerModule::OnManagedAssemblyLoaded(const FString& AssemblyName)
{
	FBlueprintCompilationManager::FlushCompilationQueueAndReinstance();
}

#undef LOCTEXT_NAMESPACE
    
IMPLEMENT_MODULE(FUnrealSharpCompilerModule, UnrealSharpCompiler)