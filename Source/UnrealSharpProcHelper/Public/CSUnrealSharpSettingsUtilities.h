#pragma once

class FCSUnrealSharpSettingsUtilities
{
public:
	static TSharedPtr<FJsonValue> GetElement(const FString& ElementName);
private:
	static void InitializeConfigFile(const FString& ProjectRoot, const FString& UnrealSharpRoot);
	
	static FString GetConfigFile(const FString& RootDirectory);
	static TMap<FString, TSharedPtr<FJsonValue>> LoadJsonAsDictionary(const FString& Path);
	
	static TMap<FString, TSharedPtr<FJsonValue>> Config;
};
