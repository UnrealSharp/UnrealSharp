#include "UGameInstanceExporter.h"
#include "CSharpForUE/CSManager.h"

void UUGameInstanceExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetGameInstanceSubsystem)
}

void* UUGameInstanceExporter::GetGameInstanceSubsystem(UClass* SubsystemClass, UObject* WorldContextObject)
{
	if (!IsValid(WorldContextObject))
	{
		return nullptr;
	}
	
	UGameInstanceSubsystem* GameInstanceSubsystem = WorldContextObject->GetWorld()->GetGameInstance()->GetSubsystemBase(SubsystemClass);
	return FCSManager::Get().FindManagedObject(GameInstanceSubsystem).GetIntPtr();
}
