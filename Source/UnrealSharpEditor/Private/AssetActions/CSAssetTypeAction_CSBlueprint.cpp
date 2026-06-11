#include "AssetActions/CSAssetTypeAction_CSBlueprint.h"

#include "CSManager.h"
#include "CSProjectUtilities.h"
#include "CSPathsUtilities.h"
#include "ReflectionData/CSClassReflectionData.h"
#include "Misc/MessageDialog.h"
#include "SourceCodeNavigation.h"
#include "Types/CSBlueprint.h"
#include "Types/CSClass.h"
#include "UnrealSharpEditor.h"
#include "Utilities/CSClassUtilities.h"

#define LOCTEXT_NAMESPACE "CSAssetTypeAction_CSBlueprint"

namespace CSAssetTypeAction_CSBlueprint_Private
{
	static FString GetProjectDirectoryForAssembly(const FName AssemblyName)
	{
		TArray<FString> ProjectPaths;
		UnrealSharp::Project::GetAllProjectPaths(ProjectPaths);

		const FString AssemblyNameString = AssemblyName.ToString();
		for (const FString& ProjectPath : ProjectPaths)
		{
			if (FPaths::GetBaseFilename(ProjectPath).Equals(AssemblyNameString, ESearchCase::IgnoreCase))
			{
				return FPaths::GetPath(ProjectPath);
			}
		}

		return FString();
	}

	static FString BuildCandidateSourcePath(const FString& ProjectDirectory, const FCSNamespace& Namespace, const FString& TypeName)
	{
		FString RelativeNamespacePath = Namespace.GetName();
		RelativeNamespacePath.ReplaceInline(TEXT("."), TEXT("/"));

		if (!RelativeNamespacePath.IsEmpty())
		{
			return FPaths::Combine(ProjectDirectory, RelativeNamespacePath, TypeName + TEXT(".cs"));
		}

		return FPaths::Combine(ProjectDirectory, TypeName + TEXT(".cs"));
	}

	static bool TryFindExactSourceFile(const FString& ProjectDirectory, const FString& FileName, FString& OutSourceFilePath)
	{
		TArray<FString> FoundFiles;
		IFileManager::Get().FindFilesRecursive(FoundFiles, *ProjectDirectory, *FileName, true, false, false);

		for (const FString& FoundFile : FoundFiles)
		{
			if (!FPaths::GetCleanFilename(FoundFile).Equals(FileName, ESearchCase::IgnoreCase))
			{
				continue;
			}

			OutSourceFilePath = FPaths::ConvertRelativePathToFull(FoundFile);
			return FPaths::FileExists(OutSourceFilePath);
		}

		return false;
	}

	static TSharedPtr<FCSManagedTypeDefinition> FindManagedTypeDefinition(const UCSBlueprint* Blueprint, UCSClass* ManagedClass)
	{
		if (ManagedClass->HasManagedTypeDefinition())
		{
			return ManagedClass->GetManagedTypeDefinition();
		}

		if (UCSManagedAssembly* OwningAssembly = UCSManager::Get().FindOwningAssembly(ManagedClass))
		{
			const FName BlueprintName = Blueprint->GetFName();
			for (const TTuple<FCSFieldName, TSharedPtr<FCSManagedTypeDefinition>>& Pair : OwningAssembly->GetDefinedManagedTypes())
			{
				if (Pair.Key.GetFName() == BlueprintName)
				{
					return Pair.Value;
				}
			}
		}

		return nullptr;
	}

	static bool TryResolveManagedSourceFilePath(const UCSBlueprint* Blueprint, FString& OutSourceFilePath)
	{
		if (!IsValid(Blueprint) || !IsValid(Blueprint->GeneratedClass))
		{
			return false;
		}

		UCSClass* ManagedClass = Cast<UCSClass>(Blueprint->GeneratedClass);
		if (!IsValid(ManagedClass) || !FCSClassUtilities::IsManagedClass(ManagedClass))
		{
			return false;
		}

		const TSharedPtr<FCSManagedTypeDefinition> TypeDefinition = FindManagedTypeDefinition(Blueprint, ManagedClass);
		if (!TypeDefinition.IsValid())
		{
			return false;
		}

		const TSharedPtr<FCSTypeReferenceReflectionData> ReflectionData = TypeDefinition->GetReflectionData<FCSTypeReferenceReflectionData>();
		if (!ReflectionData.IsValid() || ReflectionData->AssemblyName == NAME_None)
		{
			return false;
		}

		const FString ProjectDirectory = GetProjectDirectoryForAssembly(ReflectionData->AssemblyName);
		if (ProjectDirectory.IsEmpty())
		{
			return false;
		}

		const FCSFieldName& FieldName = TypeDefinition->GetFieldName();
		const FString TypeName = FieldName.GetName();
		const FString CandidatePath = BuildCandidateSourcePath(ProjectDirectory, FieldName.GetNamespace(), TypeName);
		if (FPaths::FileExists(CandidatePath))
		{
			OutSourceFilePath = FPaths::ConvertRelativePathToFull(CandidatePath);
			return true;
		}

		return TryFindExactSourceFile(ProjectDirectory, TypeName + TEXT(".cs"), OutSourceFilePath);
	}

	static void OpenManagedSourceFile(const UCSBlueprint* Blueprint)
	{
		FString SourceFilePath;
		if (!TryResolveManagedSourceFilePath(Blueprint, SourceFilePath))
		{
			UE_LOG(LogUnrealSharpEditor, Warning, TEXT("Failed to locate C# source file for blueprint '%s'"), *GetNameSafe(Blueprint));
			FMessageDialog::Open(
				EAppMsgType::Ok,
				FText::Format(
					LOCTEXT("FailedToLocateSourceFile", "Failed to locate the C# source file for '{0}'."),
					FText::FromString(GetNameSafe(Blueprint))));
			return;
		}

		if (!FSourceCodeNavigation::OpenSourceFile(SourceFilePath))
		{
			UE_LOG(LogUnrealSharpEditor, Warning, TEXT("Failed to open C# source file '%s' in the configured IDE"), *SourceFilePath);
			FMessageDialog::Open(
				EAppMsgType::Ok,
				FText::Format(
					LOCTEXT("FailedToOpenSourceFile", "Failed to open '{0}' in the configured source code editor.\n\nCheck Editor Preferences > General > Source Code and ensure Rider, Visual Studio, or Visual Studio Code is installed and selected."),
					FText::FromString(SourceFilePath)));
		}
	}
}

UClass* FCSAssetTypeAction_CSBlueprint::GetSupportedClass() const
{
	return UCSBlueprint::StaticClass();
}

#if ENGINE_MAJOR_VERSION * 100 + ENGINE_MINOR_VERSION < 505
void FCSAssetTypeAction_CSBlueprint::OpenAssetEditor(const TArray<UObject*>& InObjects, const EAssetTypeActivationOpenedMethod OpenedMethod, TSharedPtr<IToolkitHost> EditWithinLevelEditor)
#else
void FCSAssetTypeAction_CSBlueprint::OpenAssetEditor(const TArray<UObject*>& InObjects, TSharedPtr<class IToolkitHost> EditWithinLevelEditor)
#endif
{
	for (UObject* Object : InObjects)
	{
		UCSBlueprint* Blueprint = Cast<UCSBlueprint>(Object);
		if (!IsValid(Blueprint))
		{
			continue;
		}

		CSAssetTypeAction_CSBlueprint_Private::OpenManagedSourceFile(Blueprint);
	}
}

bool FCSAssetTypeAction_CSBlueprint::SupportsOpenedMethod(const EAssetTypeActivationOpenedMethod OpenedMethod) const
{
	return OpenedMethod == EAssetTypeActivationOpenedMethod::Edit
		|| OpenedMethod == EAssetTypeActivationOpenedMethod::View;
}

FText FCSAssetTypeAction_CSBlueprint::GetName() const
{
	return FText::FromString(FString::Printf(TEXT("C# Class")));
}

#undef LOCTEXT_NAMESPACE
