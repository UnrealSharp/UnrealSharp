#include "AActorExporter.h"
#include "CSharpForUE/CSManager.h"

void UAActorExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetInputComponent)
}

void* UAActorExporter::GetInputComponent(const AActor* Actor)
{
	if (!IsValid(Actor) || !IsValid(Actor->InputComponent))
	{
		return nullptr;
	}
	
	return FCSManager::Get().FindManagedObject(Actor->InputComponent).GetIntPtr();
}
