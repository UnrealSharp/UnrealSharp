#include "Export/UClassExporter.h"

#include "CSManagedAssembly.h"
#include "CSManager.h"
#include "UnrealSharpCore.h"

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

UFunction* UUClassExporter::GetFirstNativeImplementationFromInstanceAndName(const UObject* NativeObject, const char* FunctionName)
{
	if (!IsValid(NativeObject))
	{
		UE_LOG(LogUnrealSharp, Warning, TEXT("Failed to get NativeFunction. NativeObject is not valid."))
		return nullptr;
	}
	
	UClass* FirstNativeClass = FCSClassUtilities::GetFirstNativeClass(NativeObject->GetClass());
	return FirstNativeClass->FindFunctionByName(FunctionName);
}

void* UUClassExporter::GetDefaultFromName(const char* AssemblyName, const char* Namespace, const char* ClassName)
{
	UCSManagedAssembly* Assembly = UCSManager::Get().FindOrLoadAssembly(AssemblyName);
	FCSFieldName FieldName(ClassName, Namespace);
	
	UClass* Class = Assembly->ResolveUField<UClass>(FieldName);
	
	if (!IsValid(Class))
	{
		UE_LOGFMT(LogUnrealSharp, Warning, "Failed to get default object. ClassName: {0}", *FieldName.GetName());
		return nullptr;
	}
	
	return UCSManager::Get().FindManagedObject(Class->GetDefaultObject());
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
	
	return UCSManager::Get().FindManagedObject(CDO);
}

bool UUClassExporter::IsChildOf(UClass* ChildClass, UClass* ParentClass)
{
	 if (!IsValid(ChildClass) || !IsValid(ParentClass))
	 {
		 return false;
	 }
	
	 return ChildClass->IsChildOf(ParentClass);
}
