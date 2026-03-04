#include "CSUnrealSharpSettingsUtilities.h"
#include "CSProcUtilities.h"
#include "UnrealSharpProcHelper.h"

TMap<FString, TSharedPtr<FJsonValue>> FCSUnrealSharpSettingsUtilities::Config;

TSharedPtr<FJsonValue> FCSUnrealSharpSettingsUtilities::GetElement(const FString& ElementName)
{
#if WITH_EDITOR
	InitializeConfigFile(FPaths::ProjectConfigDir(), UCSProcUtilities::GetPluginDirectory());

	TSharedPtr<FJsonValue>* Found = Config.Find(ElementName);
	if (!Found)
	{
		UE_LOG(LogUnrealSharpProcHelper, Fatal, TEXT("Config element '%s' not found."), *ElementName);
	}

	return *Found;
#else 
	return nullptr;
#endif
}

void FCSUnrealSharpSettingsUtilities::InitializeConfigFile(const FString& ProjectRoot, const FString& UnrealSharpRoot)
{
	if (!Config.IsEmpty())
	{
		return;
	}

	const FString PluginConfigPath = GetConfigFile(UnrealSharpRoot);
	const FString ProjectConfigPath = GetConfigFile(ProjectRoot);

	Config = PluginConfigPath.IsEmpty() ? TMap<FString, TSharedPtr<FJsonValue>>() : LoadJsonAsDictionary(PluginConfigPath);

	if (!ProjectConfigPath.IsEmpty())
	{
		TMap<FString, TSharedPtr<FJsonValue>> ProjectDict = LoadJsonAsDictionary(ProjectConfigPath);
		
		for (const TPair<FString, TSharedPtr<FJsonValue>>& Kvp : ProjectDict)
		{
			Config.Add(Kvp.Key, Kvp.Value);
		}
	}
}

FString FCSUnrealSharpSettingsUtilities::GetConfigFile(const FString& RootDirectory)
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
		UE_LOGFMT(LogUnrealSharpProcHelper, Fatal, "Found multiple config files in {0}", *ConfigDirectory);
	}

	return FoundConfigs[0];
}

TMap<FString, TSharedPtr<FJsonValue>> FCSUnrealSharpSettingsUtilities::LoadJsonAsDictionary(const FString& Path)
{
	TMap<FString, TSharedPtr<FJsonValue>> Result;

	FString JsonString;
	if (!FFileHelper::LoadFileToString(JsonString, *Path))
	{
		UE_LOG(LogUnrealSharpProcHelper, Fatal, TEXT("Failed to read config file: %s"), *Path);
	}

	TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(JsonString);
	TSharedPtr<FJsonObject> JsonObject;

	if (!FJsonSerializer::Deserialize(Reader, JsonObject) || !JsonObject.IsValid())
	{
		UE_LOG(LogUnrealSharpProcHelper, Fatal, TEXT("Invalid JSON in config file: %s"), *Path);
	}

	for (const TPair<FString, TSharedPtr<FJsonValue>>& Pair : JsonObject->Values)
	{
		Result.Add(Pair.Key, Pair.Value);
	}

	return Result;
}