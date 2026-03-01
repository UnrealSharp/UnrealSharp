#include "UnrealSharpRuntimeGlue.h"
#include "CSGlueGenerator.h"
#include "CSProcUtilities.h"
#include "CSRuntimeGlueCommands.h"
#include "CSRuntimeGlueSettings.h"
#include "UnrealSharpEditor.h"
#include "Logging/StructuredLog.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpRuntimeGlueModule"

DEFINE_LOG_CATEGORY(LogUnrealSharpRuntimeGlue);

void FUnrealSharpRuntimeGlueModule::StartupModule()
{
	FUnrealSharpEditorModule& UnrealSharpEditor = FUnrealSharpEditorModule::Get();
	UnrealSharpEditor.OnBuildingToolbarEvent().AddStatic(&FUnrealSharpRuntimeGlueModule::OnBuildingToolbar);
	UnrealSharpEditor.AddNewProject(GetRuntimeGlueName(), UCSProcUtilities::GetScriptFolderDirectory(), FPaths::ProjectDir(), {}, false);
	
	FModuleManager::Get().OnModulesChanged().AddRaw(this, &FUnrealSharpRuntimeGlueModule::OnModulesChanged);
	
	InitializeRuntimeGlueGenerators();
	InitializeCommands();
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

FString FUnrealSharpRuntimeGlueModule::GetGlueFolder()
{
	return FPaths::Combine(UCSProcUtilities::GetScriptFolderDirectory(), GetRuntimeGlueName());
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

void FUnrealSharpRuntimeGlueModule::InitializeCommands()
{
	FCSRuntimeGlueCommands::Register();
	RuntimeGlueCommands = MakeShareable(new FUICommandList);
	RuntimeGlueCommands->MapAction(FCSRuntimeGlueCommands::Get().RefreshRuntimeGlue, FExecuteAction::CreateRaw(this, &FUnrealSharpRuntimeGlueModule::ForceRefreshRuntimeGlue));
}

void FUnrealSharpRuntimeGlueModule::OnBuildingToolbar(FMenuBuilder& MenuBuilder)
{
	MenuBuilder.BeginSection("Glue", LOCTEXT("Glue", "Glue"));
	
	MenuBuilder.AddMenuEntry(FCSRuntimeGlueCommands::Get().RefreshRuntimeGlue, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
	                         FSlateIcon(FAppStyle::GetAppStyleSetName(), "SourceControl.Actions.Refresh"));
	
	MenuBuilder.EndSection();
}

#undef LOCTEXT_NAMESPACE
    
IMPLEMENT_MODULE(FUnrealSharpRuntimeGlueModule, UnrealSharpRuntimeGlue)