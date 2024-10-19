#include "UWidgetBlueprintLibraryExporter.h"
#include "Blueprint/UserWidget.h"
#include "Blueprint/WidgetBlueprintLibrary.h"
#include "UnrealSharpCore/CSManager.h"

void UUWidgetBlueprintLibraryExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(CreateWidget)
}

void* UUWidgetBlueprintLibraryExporter::CreateWidget(UObject* WorldContextObject, UClass* WidgetClass, APlayerController* OwningPlayer)
{
	if (!IsValid(WorldContextObject) || !IsValid(WidgetClass))
	{
		return nullptr;
	}
		
	UUserWidget* UserWidget = UWidgetBlueprintLibrary::Create(WorldContextObject, WidgetClass, OwningPlayer);
	return UCSManager::Get().FindManagedObject(UserWidget).GetIntPtr();
}
