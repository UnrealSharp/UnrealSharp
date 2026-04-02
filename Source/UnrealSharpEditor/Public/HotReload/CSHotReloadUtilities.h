#pragma once
#include "CSHotReloadSubsystem.h"
#include "IDirectoryWatcher.h"
#include "UnrealSharpEditor.h"

class UCSManagedAssembly;
struct FFileChangeData;

namespace FCSHotReloadUtilities
{
	struct FCSChangedFile
	{
		FCSChangedFile(const FString& InFilePath, const FFileChangeData& InChangeData) : FilePath(InFilePath), ChangeData(InChangeData)
		{
		}
		
		FString FilePath;
		const FFileChangeData& ChangeData;
	};

	inline bool IsCSharpFile(const FString& Path) { return Path.EndsWith(TEXT(".cs")); }
	inline bool IsSkippablePath(const FString& Path) { return Path.Contains(TEXT("/obj/")) || Path.Contains(TEXT("/bin/")) || !IsCSharpFile(Path); }

	void SortDirtiedFiles(TArray<FCSChangedFile>& Files);
	
	bool HasFileBeenDirtied(const TArray<FCSChangedFile>& DirtiedFiles, const FString& FilePath, FFileChangeData::EFileChangeAction Action);

	void CollectDirtiedFiles(const TArray<FFileChangeData>& ChangedFiles, TArray<FCSChangedFile>& OutDirtied);
	bool ApplyDirtiedFiles(const FString& ProjectName, const TArray<FCSChangedFile>& DirtyFiles, FString& OutException);
	
	bool RecompileDirtyProjects(const TArray<UCSManagedAssembly*>& Assemblies, FString& OutExceptionMessage);
	
	void RebuildDependentBlueprints(const TSet<uint32>& RebuiltTypes);
	void RefreshPlacementMode();
	
	bool IsPinAffectedByReload(const FEdGraphPinType& PinType, const TSet<uint32>& RebuiltTypes);
	bool IsNodeAffectedByReload(const UEdGraphNode* Node, const TSet<uint32>& RebuiltTypes);
	bool HasDefaultComponentsBeenAffected(const UBlueprint* Blueprint, const TSet<uint32>& RebuiltTypes);
	
	void GetChangedCSharpFiles(const TArray<FFileChangeData>& ChangedFiles, TArray<FFileChangeData>& OutFilteredFiles);
	
	bool ShouldDeferHotReloadRequest(const UCSManagedAssembly* ModifiedAssembly);
	bool ShouldHotReloadOnEditorFocus(const UCSHotReloadSubsystem* HotReloadSubsystem);
};
