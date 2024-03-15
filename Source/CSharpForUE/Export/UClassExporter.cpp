#include "UClassExporter.h"
#include "CSharpForUE/TypeGenerator/Register/TypeInfo/CSClassInfo.h"
#include "CSharpForUE/TypeGenerator/Register/CSTypeRegistry.h"

void UUClassExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetDefaultFromString)
	EXPORT_FUNCTION(GetDefaultFromInstance)
	EXPORT_FUNCTION(GetNativeFunctionFromClassAndName)
	EXPORT_FUNCTION(GetNativeFunctionFromInstanceAndName)
}

UFunction* UUClassExporter::GetNativeFunctionFromClassAndName(const UClass* Class, const char* FunctionName)
{
	check(Class)
	UFunction* Function = Class->FindFunctionByName(FunctionName);
	check(Function)
	return Function;
}

UFunction* UUClassExporter::GetNativeFunctionFromInstanceAndName(const UObject* NativeObject, const char* FunctionName)
{
	check(NativeObject)
	return NativeObject->FindFunctionChecked(FunctionName);
}

void* UUClassExporter::GetDefaultFromString(const char* ClassName)
{
	if (!ClassName)
	{
		return nullptr;
	}
	
	const FCSharpClassInfo* ClassInfo = FCSTypeRegistry::GetClassInfoFromName(ClassName);

	if (!ClassInfo)
	{
		return nullptr;
	}

	UObject* CDO = ClassInfo->Field->GetDefaultObject();
	return FCSManager::Get().FindManagedObject(CDO).GetIntPtr();
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
	
	return FCSManager::Get().FindManagedObject(CDO).GetIntPtr();
}
