#include "UWidgetBlueprintLibraryExporter.h"
#include "Blueprint/UserWidget.h"
#include "Blueprint/WidgetBlueprintLibrary.h"
#include "CSharpForUE/CSManager.h"

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
	return FCSManager::Get().FindManagedObject(UserWidget).GetIntPtr();
}
