#pragma once

#include "CoreMinimal.h"
#include "CSNamespace.h"
#include "CSTypeWizardOption.h"

class FCSTypeGenerator
{
public:
	static bool GenerateTypeFile(const FCSNewTypeParams& Params, FString& OutFilePath, FText& OutFailReason);
	static FString BuildSource(const FCSNewTypeParams& Params);
	static FString GetParentTypeName(const UClass* ParentClass);
	static FCSNamespace GetManagedNamespace(const UClass* Class);
	static FString DeriveNamespaceFromPath(const FString& Directory);
	static bool IsValidCSharpIdentifier(const FString& Name, FText& OutFailReason);
	static bool IsDirectoryInAProject(const FString& FilePath);
};
