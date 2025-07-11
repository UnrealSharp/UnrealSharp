#include "UnrealSharpRuntimeGlue.h"

#include "CSGlueGenerator.h"
#include "CSRuntimeGlueSettings.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpRuntimeGlueModule"

DEFINE_LOG_CATEGORY(LogUnrealSharpRuntimeGlue);

void FUnrealSharpRuntimeGlueModule::StartupModule()
{
	FModuleManager::Get().OnModulesChanged().AddRaw(this, &FUnrealSharpRuntimeGlueModule::OnModulesChanged);
	InitializeRuntimeGlueGenerators();
}

void FUnrealSharpRuntimeGlueModule::ShutdownModule()
{

}

void FUnrealSharpRuntimeGlueModule::ForceRefreshRuntimeGlue()
{
	for (const TPair<TObjectKey<UClass>, UCSGlueGenerator*>& Pair : RuntimeGlueGenerators)
	{
		if (!IsValid(Pair.Value))
		{
			UObject* Object = Pair.Value;
			UE_LOGFMT(LogUnrealSharpRuntimeGlue, Warning, "Runtime glue generator {0} is not valid. Skipping refresh", *Object->GetName());
			continue;
		}

		Pair.Value->ForceRefresh();
	}
}

void FUnrealSharpRuntimeGlueModule::InitializeRuntimeGlueGenerators()
{
	const UCSRuntimeGlueSettings* Settings = GetDefault<UCSRuntimeGlueSettings>();

	for (const TSoftClassPtr<UCSGlueGenerator>& Generator : Settings->Generators)
	{
		if (Generator.IsNull())
		{
			UE_LOGFMT(LogUnrealSharpRuntimeGlue, Log, "{0} is null. Can't create generator", *Generator.ToString());
			continue;
		}

		UClass* GeneratorClass = Generator.Get();
		if (!GeneratorClass || RuntimeGlueGenerators.Contains(GeneratorClass))
		{
			continue;
		}

		UCSGlueGenerator* GeneratorInstance = NewObject<UCSGlueGenerator>(GetTransientPackage(), GeneratorClass, NAME_None, RF_Standalone);
		RuntimeGlueGenerators.Add(GeneratorClass, GeneratorInstance);

		GeneratorInstance->Initialize();
	}
}

void FUnrealSharpRuntimeGlueModule::OnModulesChanged(FName ModuleName, EModuleChangeReason Reason)
{
	if (Reason != EModuleChangeReason::ModuleLoaded)
	{
		return;
	}

	InitializeRuntimeGlueGenerators();
}

#undef LOCTEXT_NAMESPACE
    
IMPLEMENT_MODULE(FUnrealSharpRuntimeGlueModule, UnrealSharpRuntimeGlue)