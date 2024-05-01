#include "AActorExporter.h"
#include "CSManager.h"
#include "Components/InputComponent.h"

void UAActorExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetRootComponent);
}

void* UAActorExporter::GetRootComponent(AActor* Actor)
{
	if (!IsValid(Actor))
	{
		return nullptr;
	}
	
	return FCSManager::Get().FindManagedObject(Actor->GetRootComponent()).GetIntPtr();
}
