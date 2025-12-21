#include "UnrealSharpCompiler.h"

#include "BlueprintActionDatabase.h"
#include "BlueprintCompilationManager.h"
#include "CSBlueprintCompiler.h"
#include "CSCompilerContext.h"
#include "CSManager.h"
#include "KismetCompiler.h"
#include "AssetRegistry/AssetRegistryModule.h"
#include "AssetRegistry/IAssetRegistry.h"
#include "Types/CSBlueprint.h"
#include "Types/CSClass.h"
#include "Types/CSEnum.h"
#include "Types/CSInterface.h"
#include "Types/CSScriptStruct.h"
#include "UnrealSharpUtils.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpCompilerModule"

DEFINE_LOG_CATEGORY(LogUnrealSharpCompiler);

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
	CSManager.OnNewEnumEvent().AddRaw(this, &FUnrealSharpCompilerModule::OnNewEnum);
	CSManager.OnNewStructEvent().AddRaw(this, &FUnrealSharpCompilerModule::OnNewStruct);
	CSManager.OnManagedAssemblyLoadedEvent().AddRaw(this, &FUnrealSharpCompilerModule::OnManagedAssemblyLoaded);
	
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
	
	if (ManagedComponentsToCompile.IsEmpty() && ManagedClassesToCompile.IsEmpty())
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

			if (!Blueprint)
			{
				UE_LOGFMT(LogUnrealSharpCompiler, Error, "Blueprint is null, skipping compilation.");
				continue;
			}
			
			if (!IsValid(Blueprint))
			{
				UE_LOGFMT(LogUnrealSharpCompiler, Error, "Blueprint {0} is garbage, skipping compilation.", *Blueprint->GetName());
				continue;
			}

			constexpr EBlueprintCompileOptions Flags = EBlueprintCompileOptions::SkipGarbageCollection | EBlueprintCompileOptions::SkipSave;
			FKismetEditorUtilities::CompileBlueprint(Blueprint, Flags);

			if (!FCSUnrealSharpUtils::IsEngineStartingUp())
			{
				InvalidateReferences(Blueprint);
			}
		}
		
		Blueprints.Reset();
	};

	// Components needs be compiled first, as they are instantiated by the owning actor, and needs their size to be known.
	CompileBlueprints(ManagedComponentsToCompile);
	CompileBlueprints(ManagedClassesToCompile);

	UE_LOG(LogUnrealSharpCompiler, Log, TEXT("Recompiled and reinstanced blueprints in %.2f seconds"), FPlatformTime::Seconds() - StartTime);
}

void FUnrealSharpCompilerModule::AddManagedReferences(FCSManagedReferencesCollection& Collection)
{
	Collection.ForEachManagedReference([&](UStruct* Struct)
	{
		if (UCSClass* Class = Cast<UCSClass>(Struct))
		{
			OnNewClass(Class);
		}
	});
}

void FUnrealSharpCompilerModule::InvalidateReferences(UBlueprint* Blueprint)
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

void FUnrealSharpCompilerModule::OnNewClass(UCSClass* NewClass)
{
	UCSBlueprint* Blueprint = Cast<UCSBlueprint>(NewClass->ClassGeneratedBy);
	if (!IsValid(Blueprint))
	{
		return;
	}

	auto AddToCompileList = [this](TArray<UCSBlueprint*>& List, UCSBlueprint* Blueprint)
	{
		if (List.Contains(Blueprint))
		{
			return;
		}

		List.Add(Blueprint);
	};
		
	if (NewClass->IsChildOf(UActorComponent::StaticClass()))
	{
		AddToCompileList(ManagedComponentsToCompile, Blueprint);
	}
	else
	{
		AddToCompileList(ManagedClassesToCompile, Blueprint);
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
	UClass* DefinitionClass = Cast<UClass>(ManagedTypeDefinition->GetDefinitionField());
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
		DerivedClassInfo->MarkStructurallyDirty();
	}
}

void FUnrealSharpCompilerModule::OnManagedAssemblyLoaded(const UCSManagedAssembly* Assembly)
{
	RecompileAndReinstanceBlueprints();
}

#undef LOCTEXT_NAMESPACE
    
IMPLEMENT_MODULE(FUnrealSharpCompilerModule, UnrealSharpCompiler)