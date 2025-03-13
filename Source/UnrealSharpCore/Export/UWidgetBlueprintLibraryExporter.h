#pragma once

#include "CoreMinimal.h"
#include "UnrealSharpBinds.h"
#include "UWidgetBlueprintLibraryExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UUWidgetBlueprintLibraryExporter : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARP_FUNCTION()
	static void* CreateWidget(UObject* WorldContextObject, UClass* WidgetClass, APlayerController* OwningPlayer);
};
