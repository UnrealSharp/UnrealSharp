#pragma once

#include "CoreMinimal.h"

struct FCSModule
{
	FCSModule(FName ModuleName, const FString& SourceDirectory);

	const FString& GetGeneratedSourceDirectory() const { return Directory; }
	const FString& GetNamespace() const { return Namespace; }
	const FName& GetModuleName() const { return ModuleName; }
	const TSet<FName>& GetReferencedModules() const { return ReferencedModules; }

	void AddReferencedModule(const FName& InModuleName);

	static void CreateCSProjectFileContent(const FString& ReferencedModules, FString& CSProjectFileContent);

private:

	TSet<FName> ReferencedModules;

	FString Directory;
	FName ModuleName;
	FString Namespace;
	
};
