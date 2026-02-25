// Fill out your copyright notice in the Description page of Project Settings.

#include "DefaultGenerators/CSAssetManagerGlueGenerator.h"
#include "UnrealSharpRuntimeGlue.h"
#include "Engine/AssetManager.h"
#include "Engine/AssetManagerSettings.h"
#include "Utilities/CSEditorUtilities.h"

void UCSAssetManagerGlueGenerator::Initialize()
{
	if (UAssetManager::IsInitialized())
	{
		TryRegisterAssetTypes();
	}
	else
	{
		FModuleManager::Get().OnModulesChanged().AddUObject(this, &UCSAssetManagerGlueGenerator::OnModulesChanged);
	}
}

void UCSAssetManagerGlueGenerator::TryRegisterAssetTypes()
{
	if (bHasRegisteredAssetTypes || !UAssetManager::IsInitialized())
	{
		return;
	}

	UAssetManager::Get().CallOrRegister_OnCompletedInitialScan(FSimpleMulticastDelegate::FDelegate::CreateUObject(this, &ThisClass::OnCompletedInitialScan));
	bHasRegisteredAssetTypes = true;
}

void UCSAssetManagerGlueGenerator::OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason)
{
	if (InModuleChangeReason != EModuleChangeReason::ModuleLoaded)
	{
		return;
	}

	TryRegisterAssetTypes();
}

void UCSAssetManagerGlueGenerator::OnCompletedInitialScan()
{
	IAssetRegistry& AssetRegistry = FModuleManager::LoadModuleChecked<FAssetRegistryModule>("AssetRegistry").Get();
	AssetRegistry.OnAssetRemoved().AddUObject(this, &ThisClass::OnAssetRemoved);
	AssetRegistry.OnAssetRenamed().AddUObject(this, &ThisClass::OnAssetRenamed);
	AssetRegistry.OnInMemoryAssetCreated().AddUObject(this, &ThisClass::OnInMemoryAssetCreated);
	AssetRegistry.OnInMemoryAssetDeleted().AddUObject(this, &ThisClass::OnInMemoryAssetDeleted);

	UAssetManager::Get().Register_OnAddedAssetSearchRoot(FOnAddedAssetSearchRoot::FDelegate::CreateUObject(this, &ThisClass::OnAssetSearchRootAdded));

	UAssetManagerSettings* Settings = UAssetManagerSettings::StaticClass()->GetDefaultObject<UAssetManagerSettings>();
	Settings->OnSettingChanged().AddUObject(this, &ThisClass::OnAssetManagerSettingsChanged);

	ForceRefresh();
}

void UCSAssetManagerGlueGenerator::OnAssetRemoved(const FAssetData& AssetData)
{
	if (!IsRegisteredAssetType(AssetData))
	{
		return;
	}
	
	WaitUpdateAssetTypes();
}

void UCSAssetManagerGlueGenerator::OnAssetRenamed(const FAssetData& AssetData, const FString& OldObjectPath)
{
	OnAssetRemoved(AssetData);
}

void UCSAssetManagerGlueGenerator::OnInMemoryAssetCreated(UObject* Object)
{
	if (!IsRegisteredAssetType(Object))
	{
		return;
	}
	
	WaitUpdateAssetTypes();
}

void UCSAssetManagerGlueGenerator::OnInMemoryAssetDeleted(UObject* Object)
{
	OnInMemoryAssetCreated(Object);
}

void UCSAssetManagerGlueGenerator::OnAssetSearchRootAdded(const FString& RootPath)
{
	WaitUpdateAssetTypes();
}

void UCSAssetManagerGlueGenerator::OnAssetManagerSettingsChanged(UObject* Object,
	FPropertyChangedEvent& PropertyChangedEvent)
{
	WaitUpdateAssetTypes();
	GEditor->GetTimerManager()->SetTimerForNextTick(FTimerDelegate::CreateUObject(this, &ThisClass::ProcessAssetTypes));
}

bool UCSAssetManagerGlueGenerator::IsRegisteredAssetType(UClass* Class)
{
	if (!IsValid(Class))
	{
		return false;
	}

	UAssetManager& AssetManager = UAssetManager::Get();
	const UAssetManagerSettings& Settings = AssetManager.GetSettings();

	bool bIsPrimaryAsset = false;
	for (const FPrimaryAssetTypeInfo& PrimaryAssetType : Settings.PrimaryAssetTypesToScan)
	{
		if (Class->IsChildOf(PrimaryAssetType.GetAssetBaseClass().Get()))
		{
			bIsPrimaryAsset = true;
			break;
		}
	}
	return bIsPrimaryAsset;
}

void UCSAssetManagerGlueGenerator::ProcessAssetIds()
{
	UAssetManager& AssetManager = UAssetManager::Get();
	const UAssetManagerSettings& Settings = AssetManager.GetSettings();

	FCSScriptBuilder ScriptBuilder(FCSScriptBuilder::IndentType::Tabs);
	ScriptBuilder.AppendLine();
	ScriptBuilder.AppendLine(TEXT("using UnrealSharp.CoreUObject;"));
	ScriptBuilder.AppendLine();
	ScriptBuilder.AppendLine(TEXT("public static class AssetIds"));
	ScriptBuilder.OpenBrace();
	
	TArray<FPrimaryAssetTypeInfo> SortedPrimaryAssetTypes = Settings.PrimaryAssetTypesToScan;
	SortedPrimaryAssetTypes.Sort([](const FPrimaryAssetTypeInfo& A, const FPrimaryAssetTypeInfo& B)
	{
		return A.PrimaryAssetType.LexicalLess(B.PrimaryAssetType);
	});

	for (const FPrimaryAssetTypeInfo& PrimaryAssetType : SortedPrimaryAssetTypes)
	{
		TArray<FPrimaryAssetId> PrimaryAssetIdList;
		AssetManager.GetPrimaryAssetIdList(PrimaryAssetType.PrimaryAssetType, PrimaryAssetIdList);
		
		PrimaryAssetIdList.Sort([](const FPrimaryAssetId& A, const FPrimaryAssetId& B)
		{
			return A.PrimaryAssetName.LexicalLess(B.PrimaryAssetName);
		});
		
		for (const FPrimaryAssetId& AssetType : PrimaryAssetIdList)
		{
			FString PrimaryAssetName = AssetType.PrimaryAssetName.ToString();
			PrimaryAssetName = PrimaryAssetName.Replace(TEXT("Default__"), TEXT(""));
			
			FString AssetName = PrimaryAssetType.PrimaryAssetType.ToString() + TEXT(".") + PrimaryAssetName;
			AssetName = FCSEditorUtilities::ReplaceSpecialCharacters(AssetName);
			AssetName.RemoveFromEnd(TEXT("_C"));
			
			ScriptBuilder.AppendLine(FString::Printf(
				TEXT("public static readonly FPrimaryAssetId %s = new(\"%s\", \"%s\");"),
				*AssetName, *AssetType.PrimaryAssetType.GetName().ToString(), *AssetType.PrimaryAssetName.ToString()));
		}
	}

	ScriptBuilder.CloseBrace();
	SaveRuntimeGlue(ScriptBuilder, TEXT("AssetIds"));
}

void UCSAssetManagerGlueGenerator::ProcessAssetTypes()
{
	UAssetManager& AssetManager = UAssetManager::Get();
	const UAssetManagerSettings& Settings = AssetManager.GetSettings();

	FCSScriptBuilder ScriptBuilder(FCSScriptBuilder::IndentType::Tabs);
	ScriptBuilder.AppendLine();
	ScriptBuilder.AppendLine(TEXT("using UnrealSharp.CoreUObject;"));
	ScriptBuilder.AppendLine();
	ScriptBuilder.AppendLine(TEXT("public static class AssetTypes"));
	ScriptBuilder.OpenBrace();
	
	TArray<FPrimaryAssetTypeInfo> SortedPrimaryAssetTypes = Settings.PrimaryAssetTypesToScan;
	SortedPrimaryAssetTypes.Sort([](const FPrimaryAssetTypeInfo& A, const FPrimaryAssetTypeInfo& B)
	{
		return A.PrimaryAssetType.LexicalLess(B.PrimaryAssetType);
	});

	for (const FPrimaryAssetTypeInfo& PrimaryAssetType : SortedPrimaryAssetTypes)
	{
		FString AssetTypeName = FCSEditorUtilities::ReplaceSpecialCharacters(PrimaryAssetType.PrimaryAssetType.ToString());

		ScriptBuilder.AppendLine(FString::Printf(TEXT("public static readonly FPrimaryAssetType %s = new(\"%s\");"),
		                                         *AssetTypeName, *PrimaryAssetType.PrimaryAssetType.ToString()));
	}

	ScriptBuilder.CloseBrace();
	SaveRuntimeGlue(ScriptBuilder, TEXT("AssetTypes"));
}
