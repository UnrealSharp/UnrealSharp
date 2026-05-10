#include "CSGlobalSettingsUtilities.h"
#include "CSPathsBlueprintFunctionLibrary.h"
#include "UnrealSharpUtilities.h"
#include "Logging/StructuredLog.h"

static TMap<FString, TSharedPtr<FJsonValue>> Config;

TSharedPtr<FJsonValue> UnrealSharp::GlobalSettings::GetElement(const FString& ElementName)
{
#if WITH_EDITOR
	Private::InitializeConfigFile(FPaths::ProjectDir(), UCSPathsBlueprintFunctionLibrary::GetPluginDirectory());

	TSharedPtr<FJsonValue>* Found = Config.Find(ElementName);
	if (!Found)
	{
		UE_LOG(LogUnrealSharpUtilities, Fatal, TEXT("Config element '%s' not found."), *ElementName);
	}

	return *Found;
#else 
	return nullptr;
#endif
}

void UnrealSharp::GlobalSettings::Private::InitializeConfigFile(const FString& ProjectRoot, const FString& UnrealSharpRoot)
{
	if (!Config.IsEmpty())
	{
		return;
	}
	
	const FString PluginConfigPath = GetConfigFile(UnrealSharpRoot);
	Config = LoadJsonAsDictionary(PluginConfigPath);

	const FString ProjectOverrideConfigPath = GetConfigFile(ProjectRoot);
	if (!ProjectOverrideConfigPath.IsEmpty())
	{
		TMap<FString, TSharedPtr<FJsonValue>> ProjectOverrides = LoadJsonAsDictionary(ProjectOverrideConfigPath);
		
		for (const TPair<FString, TSharedPtr<FJsonValue>>& ProjectOverrideKVP : ProjectOverrides)
		{
			Config.Add(ProjectOverrideKVP.Key, ProjectOverrideKVP.Value);
		}
	}
}

FString UnrealSharp::GlobalSettings::Private::GetConfigFile(const FString& RootDirectory)
{
	const FString ConfigDirectory = FPaths::Combine(RootDirectory, TEXT("Config"));

	TArray<FString> FoundConfigs;
	IFileManager::Get().FindFilesRecursive(FoundConfigs, *ConfigDirectory, TEXT("UnrealSharp.Settings.json"), true, false);

	if (FoundConfigs.Num() == 0)
	{
		return FString();
	}
	
	if (FoundConfigs.Num() > 1)
	{
		UE_LOGFMT(LogUnrealSharpUtilities, Fatal, "Found multiple config files in {0}", *ConfigDirectory);
	}

	return FoundConfigs[0];
}

TMap<FString, TSharedPtr<FJsonValue>> UnrealSharp::GlobalSettings::Private::LoadJsonAsDictionary(const FString& Path)
{
	TMap<FString, TSharedPtr<FJsonValue>> Result;

	FString JsonString;
	if (!FFileHelper::LoadFileToString(JsonString, *Path))
	{
		UE_LOG(LogUnrealSharpUtilities, Fatal, TEXT("Failed to read config file: %s"), *Path);
	}

	TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(JsonString);
	TSharedPtr<FJsonObject> JsonObject;

	if (!FJsonSerializer::Deserialize(Reader, JsonObject) || !JsonObject.IsValid())
	{
		UE_LOG(LogUnrealSharpUtilities, Fatal, TEXT("Invalid JSON in config file: %s"), *Path);
	}

	for (const TPair<FString, TSharedPtr<FJsonValue>>& Pair : JsonObject->Values)
	{
		Result.Add(Pair.Key, Pair.Value);
	}

	return Result;
}
