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

	FCSTypeRegistry& TypeRegistry = FCSTypeRegistry::Get();
	TypeRegistry.GetOnNewClassEvent().AddRaw(this, &FUnrealSharpCompilerModule::OnNewClass);
	TypeRegistry.GetOnPendingClassesProcessedEvent().AddRaw(this, &FUnrealSharpCompilerModule::RecompileAndReinstanceBlueprints);
	UCSManager::GetOrCreate().OnManagedAssemblyLoadedEvent().AddRaw(this, &FUnrealSharpCompilerModule::OnManagedAssemblyLoaded);

	// Try to recompile and reinstance all blueprints when the module is loaded.
	RecompileAndReinstanceBlueprints();
}

void FUnrealSharpCompilerModule::ShutdownModule()
{
    
}

void FUnrealSharpCompilerModule::RecompileAndReinstanceBlueprints()
{
	auto CompileBlueprints = [](TArray<UBlueprint*>& Blueprints) -> void
	{
		for (UBlueprint* Blueprint : Blueprints)
		{
			FBlueprintCompilationManager::QueueForCompilation(Blueprint);
		}
		
		FBlueprintCompilationManager::FlushCompilationQueueAndReinstance();
		Blueprints.Empty();
	};
	
	// Components need to be compiled first, as they can be dependencies for actors as components.
	CompileBlueprints(ManagedComponentsToCompile);
	CompileBlueprints(OtherManagedClasses);
}


void FUnrealSharpCompilerModule::OnNewClass(UClass* NewClass)
{
	if (UBlueprint* Blueprint = Cast<UBlueprint>(NewClass->ClassGeneratedBy))
	{
		if (NewClass->IsChildOf(UActorComponent::StaticClass()))
		{
			ManagedComponentsToCompile.Add(Blueprint);
		}
		else
		{
			OtherManagedClasses.Add(Blueprint);
		}
	}
}

void FUnrealSharpCompilerModule::OnManagedAssemblyLoaded(const FString& AssemblyName)
{
	RecompileAndReinstanceBlueprints();
}

#undef LOCTEXT_NAMESPACE
    
IMPLEMENT_MODULE(FUnrealSharpCompilerModule, UnrealSharpCompiler)