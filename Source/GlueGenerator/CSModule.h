#pragma once

#include "CoreMinimal.h"

class FXmlFile;

struct FCSModule
{
	FCSModule(FName ModuleName, const FString& SourceDirectory);

	const FString& GetGeneratedSourceDirectory() const { return Directory; }
	const FString& GetNamespace() const { return Namespace; }
	const FName& GetModuleName() const { return ModuleName; }
	static FString& CreateCSProjectFileContent();

private:

	FString Directory;
	FName ModuleName;
	FString Namespace;
	
};
