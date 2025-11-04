#pragma once

#include "CoreMinimal.h"
#include "UnrealSharpEditor.h"
#include "UnrealSharpBinds/Public/CSBindsManager.h"
#include "UObject/Object.h"
#include "UnrealSharpEditorModuleExporter.generated.h"

UCLASS()
class UNREALSHARPEDITOR_API UFUnrealSharpEditorModuleExporter : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARP_FUNCTION()
	static void InitializeUnrealSharpEditorCallbacks(FCSManagedUnrealSharpEditorCallbacks Callbacks);

	UNREALSHARP_FUNCTION()
	static void GetProjectPaths(TArray<FString>* Paths);

	UNREALSHARP_FUNCTION()
	static void DirtyUnrealType(const char* AssemblyName, const char* Namespace, const char* TypeName);
};
