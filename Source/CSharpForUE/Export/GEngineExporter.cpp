#include "GEngineExporter.h"
#include "Engine/Engine.h"
#include "CSharpForUE/CSManager.h"

void UGEngineExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetEngineSubsystem)
}

void* UGEngineExporter::GetEngineSubsystem(UClass* SubsystemClass)
{
	UEngineSubsystem* GameInstanceSubsystem = GEngine->GetEngineSubsystemBase(SubsystemClass);
	return FCSManager::Get().FindManagedObject(GameInstanceSubsystem).GetIntPtr();
}
