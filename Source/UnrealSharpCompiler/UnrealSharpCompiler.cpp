#include "UnrealSharpCompiler.h"

#include "BlueprintCompilationManager.h"
#include "CSBlueprintCompiler.h"
#include "CSCompilerContext.h"
#include "CSManager.h"
#include "KismetCompiler.h"
#include "TypeGenerator/CSBlueprint.h"
#include "TypeGenerator/Register/CSGeneratedClassBuilder.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpCompilerModule"

void FUnrealSharpCompilerModule::StartupModule()
{
	UCSManager& CSManager = UCSManager::GetOrCreate();
	
	FKismetCompilerContext::RegisterCompilerForBP(UCSBlueprint::StaticClass(), [](UBlueprint* InBlueprint, FCompilerResultsLog& InMessageLog, const FKismetCompilerOptions& InCompileOptions)
	{
		return MakeShared<FCSCompilerContext>(CastChecked<UCSBlueprint>(InBlueprint), InMessageLog, InCompileOptions);
	});
	
	IKismetCompilerInterface& KismetCompilerModule = FModuleManager::LoadModuleChecked<IKismetCompilerInterface>("KismetCompiler");
	KismetCompilerModule.GetCompilers().Add(&BlueprintCompiler);
	
	CSManager.OnNewClassEvent().AddRaw(this, &FUnrealSharpCompilerModule::OnNewClass);
	CSManager.OnProcessedPendingClassesEvent().AddRaw(this, &FUnrealSharpCompilerModule::RecompileAndReinstanceBlueprints);
	CSManager.OnManagedAssemblyLoadedEvent().AddRaw(this, &FUnrealSharpCompilerModule::OnManagedAssemblyLoaded);

	// Try to recompile and reinstance all blueprints when the module is loaded.
	CSManager.ForEachManagedPackage([this](const UPackage* Package)
	{
		ForEachObjectWithPackage(Package, [this](UObject* Object)
		{
			if (UBlueprint* Blueprint = Cast<UBlueprint>(Object))
			{
				OnNewClass(Blueprint->GeneratedClass);
			}
			return true;
		}, false);
	});
	
	RecompileAndReinstanceBlueprints();
}

void FUnrealSharpCompilerModule::ShutdownModule()
{
    
}

void FUnrealSharpCompilerModule::RecompileAndReinstanceBlueprints()
{
	auto QueueBlueprints = [](TArray<UBlueprint*>& Blueprints) -> void
	{
		for (UBlueprint* Blueprint : Blueprints)
		{
			FBlueprintCompilationManager::QueueForCompilation(Blueprint);
		}
	};
	
	// Components need to be compiled first, as they can be dependencies for actors as components.
	QueueBlueprints(ManagedComponentsToCompile);
	QueueBlueprints(OtherManagedClasses);
	
	FBlueprintCompilationManager::FlushCompilationQueueAndReinstance();

	ManagedComponentsToCompile.Empty();
	OtherManagedClasses.Empty();
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

void FUnrealSharpCompilerModule::OnManagedAssemblyLoaded(const FName& AssemblyName)
{
	RecompileAndReinstanceBlueprints();
}

#undef LOCTEXT_NAMESPACE
    
IMPLEMENT_MODULE(FUnrealSharpCompilerModule, UnrealSharpCompiler)