#pragma once

namespace UnrealSharp::GlobalSettings
{
	UNREALSHARPUTILITIES_API TSharedPtr<FJsonValue> GetElement(const FString& ElementName);
	
	namespace Private
	{
		void InitializeConfigFile(const FString& ProjectRoot, const FString& UnrealSharpRoot);
		FString GetConfigFile(const FString& RootDirectory);
		TMap<FString, TSharedPtr<FJsonValue>> LoadJsonAsDictionary(const FString& Path);
	}
}

