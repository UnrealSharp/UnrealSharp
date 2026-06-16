#include "CSProjectUtilities.h"

#include "CSCommonGlobalSettings.h"
#include "CSPathsUtilities.h"
#include "UnrealSharpUtilities.h"
#include "Interfaces/IPluginManager.h"
#include "Logging/StructuredLog.h"

void UnrealSharp::Project::DiscoverLoadOrderManifests(TArray<FCSLoadOrderManifest>& OutManifests)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UnrealSharp::Project::DiscoverLoadOrderManifests);
	
	const FString UserAssemblyDirectory = Paths::GetUserAssemblyDirectory();

	TArray<FString> ManifestFiles;
	IFileManager::Get().FindFiles(ManifestFiles, *(UserAssemblyDirectory / TEXT("*.LoadOrder.json")), true, false);

	OutManifests.Reserve(ManifestFiles.Num());

	for (const FString& File : ManifestFiles)
	{
		const FString FullPath = FPaths::Combine(UserAssemblyDirectory, File);

		FString JsonString;
		if (!FFileHelper::LoadFileToString(JsonString, *FullPath))
		{
			UE_LOGFMT(LogUnrealSharpUtilities, Warning, "Failed to load load-order manifest: {0}", FullPath);
			continue;
		}

		TSharedPtr<FJsonObject> JsonObject;
		if (!FJsonSerializer::Deserialize(TJsonReaderFactory<>::Create(JsonString), JsonObject) || !JsonObject.IsValid())
		{
			UE_LOGFMT(LogUnrealSharpUtilities, Warning, "Failed to parse load-order manifest: {0}", FullPath);
			continue;
		}

		const TArray<TSharedPtr<FJsonValue>>* Order;
		if (!JsonObject->TryGetArrayField(TEXT("LoadOrder"), Order))
		{
			UE_LOGFMT(LogUnrealSharpUtilities, Warning, "Manifest missing LoadOrder array: {0}", FullPath);
			continue;
		}

		FCSLoadOrderManifest Manifest;
		Manifest.Name = FPaths::GetBaseFilename(File);
		JsonObject->TryGetNumberField(TEXT("Priority"), Manifest.Priority);
		JsonObject->TryGetBoolField(TEXT("Collectible"), Manifest.bCollectible);

		Manifest.AssemblyPaths.Reserve(Order->Num());
		for (const TSharedPtr<FJsonValue>& Entry : *Order)
		{
			Manifest.AssemblyPaths.Add(FPaths::Combine(UserAssemblyDirectory, Entry->AsString() + TEXT(".dll")));
		}

		OutManifests.Add(MoveTemp(Manifest));
	}

	OutManifests.Sort([](const FCSLoadOrderManifest& A, const FCSLoadOrderManifest& B)
	{
		return A.Priority > B.Priority;
	});
}

bool UnrealSharp::Project::IsAssemblyInAnyManifest(const FString& AssemblyName)
{
	bool bFound = false;
	
	TArray<FCSLoadOrderManifest> Manifests;
	DiscoverLoadOrderManifests(Manifests);
	
	for (const FCSLoadOrderManifest& Manifest : Manifests)
	{
		if (!Manifest.ContainsAssembly(AssemblyName))
		{
			continue;
		}
		
		bFound = true;
		break;
	}
	
	return bFound;
}

void UnrealSharp::Project::GetAllProjectPaths(TArray<FString>& ProjectPaths)
{
	IFileManager::Get().FindFilesRecursive(ProjectPaths, *Paths::GetScriptFolderDirectory(),
	TEXT("*.csproj"),
	true,
	false,
	false);

	TArray<FString> PluginFilePaths;
	IPluginManager::Get().FindPluginsUnderDirectory(FPaths::ProjectPluginsDir(), PluginFilePaths);
	
	for (const FString& PluginFilePath : PluginFilePaths)
	{
		const FString ScriptDirectory = FPaths::GetPath(PluginFilePath) / GlobalSettings::Common::GetScriptDirectoryName();
		IFileManager::Get().FindFilesRecursive(ProjectPaths,
			*ScriptDirectory,
			TEXT("*.csproj"),
			true,
			false,
			false);
	}
}

FString UnrealSharp::Project::GetUserManagedProjectName()
{
	return FString::Printf(TEXT("Managed%s"), FApp::GetProjectName());
}
