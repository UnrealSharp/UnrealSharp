#include "CSProjectUtilities.h"

#include "CSCommonGlobalSettings.h"
#include "CSPathsUtilities.h"
#include "UnrealSharpUtilities.h"
#include "Interfaces/IPluginManager.h"
#include "Logging/StructuredLog.h"

void UnrealSharp::Project::GetProjectNamesByLoadOrder(TArray<FString>& UserProjectNames, bool bIncludeGlue)
{
	const FString ProjectMetadataPath = Paths::GetUnrealSharpMetadataPath();

	if (!FPaths::FileExists(ProjectMetadataPath))
	{
		return;
	}

	FString JsonString;
	if (!FFileHelper::LoadFileToString(JsonString, *ProjectMetadataPath))
	{
		UE_LOGFMT(LogUnrealSharpUtilities, Fatal, "Failed to load UnrealSharp metadata file at: {0}", ProjectMetadataPath);
	}

	TSharedPtr<FJsonObject> JsonObject;
	if (!FJsonSerializer::Deserialize(TJsonReaderFactory<>::Create(JsonString), JsonObject) || !JsonObject.IsValid())
	{
		UE_LOGFMT(LogUnrealSharpUtilities, Fatal, "Failed to parse UnrealSharp metadata at: {0}", ProjectMetadataPath);
	}

	const TArray<TSharedPtr<FJsonValue>>* LoadOrderArray;
	if (!JsonObject->TryGetArrayField(TEXT("LoadOrder"), LoadOrderArray))
	{
		UE_LOGFMT(LogUnrealSharpUtilities, Fatal, "Failed to find LoadOrder array in UnrealSharp metadata at: {0}", ProjectMetadataPath);
	}

	for (const TSharedPtr<FJsonValue>& OrderEntry : *LoadOrderArray)
	{
		const FString ProjectName = OrderEntry->AsString();

		if (!bIncludeGlue && ProjectName.EndsWith(TEXT(".Glue")))
		{
			continue;
		}

		UserProjectNames.Add(ProjectName);
	}
}

void UnrealSharp::Project::GetAssemblyPathsByLoadOrder(TArray<FString>& AssemblyPaths, bool bIncludeGlue)
{
	const FString AbsoluteFolderPath = Paths::GetUserAssemblyDirectory();

	TArray<FString> ProjectNames;
	GetProjectNamesByLoadOrder(ProjectNames, bIncludeGlue);

	AssemblyPaths.Reserve(ProjectNames.Num());
	for (const FString& ProjectName : ProjectNames)
	{
		AssemblyPaths.Emplace(FPaths::Combine(AbsoluteFolderPath, ProjectName + TEXT(".dll")));
	}
}

void UnrealSharp::Project::GetAllProjectPaths(TArray<FString>& ProjectPaths, bool bIncludeProjectGlue)
{
	IFileManager::Get().FindFilesRecursive(ProjectPaths,
	*Paths::GetScriptFolderDirectory(),
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

	if (!bIncludeProjectGlue)
	{
		ProjectPaths.RemoveAll([](const FString& Path)
		{
			return Path.EndsWith(TEXT("Glue.csproj"));
		});
	}
}

FString UnrealSharp::Project::GetUserManagedProjectName()
{
	return FString::Printf(TEXT("Managed%s"), FApp::GetProjectName());
}

FString UnrealSharp::Project::AppendGlueSuffix(const FString& FileName)
{
	if (FileName.EndsWith(TEXT(".Glue")))
	{
		return FileName;
	}
	return FileName + TEXT(".Glue");
}
