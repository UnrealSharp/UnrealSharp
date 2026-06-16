#include "UnrealSharpCompiler.h"

#include "BlueprintCompilationManager.h"
#include "CSBlueprintCompiler.h"
#include "CSCompilerContext.h"
#include "CSManager.h"
#include "CSProjectUtilities.h"
#include "KismetCompiler.h"
#include "AssetRegistry/AssetRegistryModule.h"
#include "AssetRegistry/IAssetRegistry.h"
#include "Types/CSBlueprint.h"
#include "Types/CSClass.h"
#include "Types/CSEnum.h"
#include "Types/CSScriptStruct.h"
#include "UnrealSharpUtils.h"
#include "Compilers/CSManagedClassCompiler.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpCompilerModule"

DEFINE_LOG_CATEGORY(LogUnrealSharpCompiler);

void FUnrealSharpCompilerModule::StartupModule()
{
	UCSManager& CSManager = UCSManager::Get();
	
	FKismetCompilerContext::RegisterCompilerForBP(UCSBlueprint::StaticClass(), [](UBlueprint* InBlueprint, FCompilerResultsLog& InMessageLog, const FKismetCompilerOptions& InCompileOptions)
	{
		return MakeShared<FCSCompilerContext>(CastChecked<UCSBlueprint>(InBlueprint), InMessageLog, InCompileOptions);
	});
	
	IKismetCompilerInterface& KismetCompilerModule = FModuleManager::LoadModuleChecked<IKismetCompilerInterface>("KismetCompiler");
	KismetCompilerModule.GetCompilers().Add(&BlueprintCompiler);
	
	CSManager.OnNewClassEvent().AddRaw(this, &FUnrealSharpCompilerModule::OnNewClass);
	CSManager.OnNewEnumEvent().AddRaw(this, &FUnrealSharpCompilerModule::OnNewEnum);
	CSManager.OnNewStructEvent().AddRaw(this, &FUnrealSharpCompilerModule::OnNewStruct);
	
	FCSAssemblyEvents::OnAssemblyLoaded.AddRaw(this, &FUnrealSharpCompilerModule::OnManagedAssemblyLoaded);
	
	FOnManagedTypeStructureChanged::FDelegate Delegate = FOnManagedTypeStructureChanged::FDelegate::CreateRaw(this, &FUnrealSharpCompilerModule::OnReflectionDataChanged);
	FCSManagedTypeDefinitionEvents::AddOnReflectionDataChangedDelegate(Delegate);
}

void FUnrealSharpCompilerModule::ShutdownModule()
{
    
}

void FUnrealSharpCompilerModule::RecompileAndReinstanceBlueprints()
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FUnrealSharpCompilerModule::RecompileAndReinstanceBlueprints);
	
	if (UCSManager::Get().IsLoadingAnyAssembly())
	{
		// Wait until all assemblies are loaded, so we can recompile all blueprints at once.
		return;
	}
	
	if (ManagedClassesToCompile.IsEmpty())
	{
		// Nothing to compile.
		return;
	}

	double StartTime = FPlatformTime::Seconds();
	
	auto CompileBlueprints = [this](TArray<UCSBlueprint*>& Blueprints)
	{
		if (Blueprints.IsEmpty())
		{
			return;
		}
		
		for (int32 i = 0; i < Blueprints.Num(); ++i)
		{
			UCSBlueprint* Blueprint = Blueprints[i];

			constexpr EBlueprintCompileOptions Flags = EBlueprintCompileOptions::SkipGarbageCollection 
			| EBlueprintCompileOptions::SkipSave 
			| EBlueprintCompileOptions::SkipDefaultObjectValidation
			| EBlueprintCompileOptions::SkipFiBSearchMetaUpdate
			| EBlueprintCompileOptions::SkipNewVariableDefaultsDetection;
			
			FKismetEditorUtilities::CompileBlueprint(Blueprint, Flags);

			if (!FCSUnrealSharpUtils::IsEngineStartingUp())
			{
				RefreshDependentLoaders(Blueprint);
				RefreshInstanceTickSettings(Blueprint);
			}
		}
		
		Blueprints.Reset();
	};

	// Components needs be compiled first, as they are instantiated by the owning actor, and needs their size to be known.
	CompileBlueprints(ManagedComponentsToCompile);
	CompileBlueprints(ManagedClassesToCompile);

	UE_LOG(LogUnrealSharpCompiler, Log, TEXT("Recompiled and reinstanced blueprints in %.2f seconds"), FPlatformTime::Seconds() - StartTime);
}

void FUnrealSharpCompilerModule::AddManagedReferences(FCSReferencesCollection& Collection)
{
	for (const TWeakObjectPtr<UStruct> Reference : Collection.GetReferences())
	{
		UCSClass* ClassReference = Cast<UCSClass>(Reference.Get());
		if (!ClassReference)
		{
			continue;
		}
		
		OnNewClass(ClassReference);
	}
}

void FUnrealSharpCompilerModule::RefreshDependentLoaders(UBlueprint* Blueprint)
{
	// This is mostly for sub-levels, not sure why but sometimes the references are not properly updated for sub-levels and causes a crash on loading the level.
	// There are probably better ways to do this.
	TRACE_CPUPROFILER_EVENT_SCOPE(FUnrealSharpCompilerModule::InvalidateReferences);
	static IAssetRegistry& AssetRegistry = FModuleManager::GetModuleChecked<FAssetRegistryModule>("AssetRegistry").Get();
			
	TArray<FName> BlueprintReferences;
	UPackage* Package = Blueprint->GetOutermost();
	AssetRegistry.GetReferencers(Package->GetFName(), BlueprintReferences, UE::AssetRegistry::EDependencyCategory::All, UE::AssetRegistry::EDependencyQuery::Hard);
	
	for (FName PackageName : BlueprintReferences)
	{
		UPackage* ReferencePackage = FindPackage(nullptr, *PackageName.ToString());
		
		if (!IsValid(ReferencePackage))
		{
			continue;
		}

		ResetLoaders(ReferencePackage);
	}
}

void FUnrealSharpCompilerModule::RefreshInstanceTickSettings(const UBlueprint* Blueprint)
{
	UClass* GeneratedClass = Blueprint->GeneratedClass;
	
	TArray<UObject*> Objects;
	GetObjectsOfClass(GeneratedClass, Objects, false, RF_NoFlags);
	
	for (UObject* Object : Objects)
	{
		UCSManagedClassCompiler::SetupDefaultTickSettings(Object, GeneratedClass);
	}
}

void FUnrealSharpCompilerModule::OnNewClass(UCSClass* NewClass)
{
	UCSBlueprint* Blueprint = static_cast<UCSBlueprint*>(NewClass->ClassGeneratedBy);
	if (NewClass->IsChildOf(UActorComponent::StaticClass()))
	{
		ManagedComponentsToCompile.Add(Blueprint);
	}
	else
	{
		ManagedClassesToCompile.Add(Blueprint);
	}

	AddManagedReferences(NewClass->GetManagedReferencesCollection());
}

void FUnrealSharpCompilerModule::OnNewStruct(UCSScriptStruct* NewStruct)
{
	AddManagedReferences(NewStruct->GetManagedReferencesCollection());
}

void FUnrealSharpCompilerModule::OnNewEnum(UCSEnum* NewEnum)
{
	AddManagedReferences(NewEnum->GetManagedReferencesCollection());
}

void FUnrealSharpCompilerModule::OnReflectionDataChanged(TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition)
{
	UClass* DefinitionClass = Cast<UClass>(ManagedTypeDefinition->GetDefinition());
	if (!IsValid(DefinitionClass))
	{
		return;
	}
	
	TArray<UClass*> DerivedClasses;
	GetDerivedClasses(DefinitionClass, DerivedClasses, false);

	for (UClass* DerivedClass : DerivedClasses)
	{
		if (!FCSClassUtilities::IsManagedClass(DerivedClass))
		{
			continue;
		}
		
		UCSClass* ManagedClass = static_cast<UCSClass*>(DerivedClass);
		TSharedPtr<FCSManagedTypeDefinition> DerivedClassInfo = ManagedClass->GetManagedTypeDefinition();
		DerivedClassInfo->SetDirtyFlags(ManagedTypeDefinition->GetDirtyFlags());
	}
}

void FUnrealSharpCompilerModule::OnManagedAssemblyLoaded(UCSManagedAssembly* Assembly)
{
	if (!IsAssemblyHotReloadable(Assembly))
	{
		return;
	}
	
	RecompileAndReinstanceBlueprints();
}

bool FUnrealSharpCompilerModule::IsAssemblyHotReloadable(const UCSManagedAssembly* Assembly)
{
	TArray<FCSLoadOrderManifest> OutManifests;
	UnrealSharp::Project::DiscoverLoadOrderManifests(OutManifests);
	
	bool CanRecompileAndReinstanceBlueprints = false;
	for (const FCSLoadOrderManifest& Manifest : OutManifests)
	{
		if (!Manifest.bCollectible || !Manifest.ContainsAssembly(Assembly->GetAssemblyFileName()))
		{
			continue;
		}
		
		CanRecompileAndReinstanceBlueprints = true;
		break;
	}
	
	return CanRecompileAndReinstanceBlueprints;
}

#undef LOCTEXT_NAMESPACE
    
IMPLEMENT_MODULE(FUnrealSharpCompilerModule, UnrealSharpCompiler)