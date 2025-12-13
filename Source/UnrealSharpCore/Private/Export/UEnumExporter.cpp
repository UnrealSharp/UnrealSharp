#include "Export/UEnumExporter.h"
#include "CSManager.h"
#include "Types/CSEnum.h"

FGCHandleIntPtr UUEnumExporter::GetManagedEnumType(UEnum* ScriptEnum)
{
	if (const UCSEnum* CSEnum = Cast<UCSEnum>(ScriptEnum); CSEnum != nullptr)
	{
		return CSEnum->GetManagedTypeDefinition()->GetTypeGCHandle()->GetHandle();
	}

	const UCSManagedAssembly* Assembly = UCSManager::Get().FindOwningAssembly(ScriptEnum);
	if (Assembly == nullptr)
	{
		return FGCHandleIntPtr();
	}

	const FCSFieldName FieldName(ScriptEnum);
	const TSharedPtr<FCSManagedTypeDefinition> Info = Assembly->FindManagedTypeDefinition(FieldName);
	if (!Info.IsValid())
	{
		return FGCHandleIntPtr();
	}

	return Info->GetTypeGCHandle()->GetHandle();   
}
