#include "Export/UCoreUObjectExporter.h"
#include "CSManagedAssembly.h"
#include "CSManager.h"
#include "Logging/StructuredLog.h"

UField* UUCoreUObjectExporter::GetType(const char* InAssemblyName, const char* InNamespace, const char* InTypeName)
{
	// This gets called by the static constructor of the type, so we can cache the type info of native classes here.
	UCSManagedAssembly* Assembly = UCSManager::Get().FindOrLoadAssembly(InAssemblyName);
	FCSFieldName FieldName(InTypeName, InNamespace);
	
	TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition = Assembly->FindOrAddManagedTypeDefinition(FieldName);
	UField* Field = ManagedTypeDefinition->CompileAndGetDefinitionField();

#if WITH_EDITOR
	if (UCSClass* Class = Cast<UCSClass>(Field))
	{
		UBlueprint* Blueprint = Cast<UBlueprint>(Class->ClassGeneratedBy);
		Field = Blueprint->SkeletonGeneratedClass;
	}
#endif
	
	if (!IsValid(Field))
	{
		UE_LOGFMT(LogUnrealSharp, Warning, "Failed to find type: {0}.{1} in assembly {2}", InNamespace, InTypeName, InAssemblyName);
		return nullptr;
	}

	return Field;
}

UField* UUCoreUObjectExporter::GetGeneratedClassFromSkeleton(UField* InType)
{
	if (!IsValid(InType))
	{
		UE_LOGFMT(LogUnrealSharp, Warning, "GetGeneratedClassFromSkeleton called with invalid type");
		return nullptr;
	}
	
	UCSSkeletonClass* SkeletonClass = Cast<UCSSkeletonClass>(InType);
	
	if (!IsValid(SkeletonClass))
	{
		return nullptr;
	}

	return SkeletonClass->GetGeneratedClass();
}
