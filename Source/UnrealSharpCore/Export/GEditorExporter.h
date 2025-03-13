#pragma once

#include "CoreMinimal.h"
#include "UnrealSharpBinds.h"
#include "GEditorExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UGEditorExporter : public UObject
{
	GENERATED_BODY()
public:
	
	UNREALSHARP_FUNCTION()
	static void* GetEditorSubsystem(UClass* SubsystemClass);
};
