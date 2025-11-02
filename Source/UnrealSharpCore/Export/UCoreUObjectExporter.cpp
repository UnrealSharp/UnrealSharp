#include "UCoreUObjectExporter.h"
#include "CSAssembly.h"
#include "CSManager.h"
#include "CSManager.h"
#include "Logging/StructuredLog.h"

UField* UUCoreUObjectExporter::GetType(const char* InAssemblyName, const char* InNamespace, const char* InTypeName)
{
	// This gets called by the static constructor of the type, so we can cache the type info of native classes here.
	UCSAssembly* Assembly = UCSManager::Get().FindOrLoadAssembly(InAssemblyName);
	FCSFieldName FieldName(InTypeName, InNamespace);
	
	TSharedPtr<FCSManagedTypeInfo> TypeInfo = Assembly->FindOrAddTypeInfo(FieldName);
	UField* Field = TypeInfo->GetOrBuildField();

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
