#include "UClassExporter.h"
#include "UnrealSharpCore/TypeGenerator/Register/TypeInfo/CSClassInfo.h"
#include "UnrealSharpCore/UnrealSharpCore.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSTypeRegistry.h"

void UUClassExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetDefaultFromName)
	EXPORT_FUNCTION(GetDefaultFromInstance)
	EXPORT_FUNCTION(GetNativeFunctionFromClassAndName)
	EXPORT_FUNCTION(GetNativeFunctionFromInstanceAndName)
}

UFunction* UUClassExporter::GetNativeFunctionFromClassAndName(const UClass* Class, const char* FunctionName)
{
	UFunction* Function = Class->FindFunctionByName(FunctionName);
	
	if (!Function)
	{
		UE_LOG(LogUnrealSharp, Warning, TEXT("Failed to get NativeFunction. FunctionName: %hs"), FunctionName)
		return nullptr;
	}

	return Function;
}

UFunction* UUClassExporter::GetNativeFunctionFromInstanceAndName(const UObject* NativeObject, const char* FunctionName)
{
	if (!IsValid(NativeObject))
	{
		UE_LOG(LogUnrealSharp, Warning, TEXT("Failed to get NativeFunction. NativeObject is not valid."))
		return nullptr;
	}
	
	return NativeObject->FindFunctionChecked(FunctionName);
}

void* UUClassExporter::GetDefaultFromName(const char* ClassName)
{
	UClass* Class = FCSTypeRegistry::Get().GetClassFromName(ClassName);
	
	if (!IsValid(Class))
	{
		ensureAlways("Failed to get Class from name");
		return nullptr;
	}

	UObject* CDO = Class->GetDefaultObject();
	return UCSManager::Get().FindManagedObject(CDO).GetIntPtr();
}

void* UUClassExporter::GetDefaultFromInstance(UObject* Object)
{
	if (!IsValid(Object))
	{
		return nullptr;
	}

	UObject* CDO;
	if (UClass* Class = Cast<UClass>(Object))
	{
		CDO = Class->GetDefaultObject();
	}
	else
	{
		CDO = Object->GetClass()->GetDefaultObject();
	}
	
	return UCSManager::Get().FindManagedObject(CDO).GetIntPtr();
}
