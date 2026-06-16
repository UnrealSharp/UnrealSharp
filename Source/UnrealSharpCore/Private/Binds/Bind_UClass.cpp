#include "CSManagedAssembly.h"
#include "CSManager.h"
#include "UnrealSharpCore.h"

DECLARE_UNREALSHARP_BINDER(Bind_UClass)
{
	UFunction* GetNativeFunctionFromClassAndName(const UClass* Class, const char* FunctionName)
	{
		if (!IsValid(Class))
		{
			UE_LOGFMT(LogUnrealSharp, Warning, "Failed to get NativeFunction for class. Class is not valid. FunctionName: {0}", FunctionName);
			return nullptr;
		}
		
		UFunction* Function = Class->FindFunctionByName(FunctionName);
		
		if (!IsValid(Function))
		{
			UE_LOGFMT(LogUnrealSharp, Warning, "Failed to get NativeFunction. Class: {0}, FunctionName: {1}", *Class->GetName(), FunctionName);
			return nullptr;
		}

		return Function;
	}

	UFunction* GetNativeFunctionFromInstanceAndName(const UObject* NativeObject, const char* FunctionName)
	{
		if (!IsValid(NativeObject))
		{
			UE_LOGFMT(LogUnrealSharp, Warning, "Failed to get NativeFunction. NativeObject is not valid. ObjectName: {0}", *NativeObject->GetName());
			return nullptr;
		}
		
		return NativeObject->FindFunctionChecked(FunctionName);
	}

	UFunction* GetFirstNativeImplementationFromInstanceAndName(const UObject* NativeObject, const char* FunctionName)
	{
		if (!IsValid(NativeObject))
		{
			UE_LOGFMT(LogUnrealSharp, Warning, "Failed to get NativeFunction. NativeObject is not valid. ObjectName: {0}", *NativeObject->GetName());
			return nullptr;
		}
		
		UClass* FirstNativeClass = FCSClassUtilities::GetFirstNativeClass(NativeObject->GetClass());
		return FirstNativeClass->FindFunctionByName(FunctionName);
	}

	void* GetDefaultFromName(const char* AssemblyName, const char* Namespace, const char* ClassName)
	{
		UCSManagedAssembly* Assembly = UCSManager::Get().FindOrLoadAssembly(AssemblyName);
		FCSFieldName FieldName(ClassName, Namespace);
		
		UClass* ResolvedClass = Assembly->ResolveUField<UClass>(FieldName);
		
		if (!IsValid(ResolvedClass))
		{
			UE_LOGFMT(LogUnrealSharp, Warning, "Failed to get default object for class {0} in assembly {1}.", *FieldName.GetName(), *AssemblyName);
			return nullptr;
		}
		
		return UCSManager::Get().FindManagedObject(ResolvedClass->GetDefaultObject());
	}

	void* GetDefaultFromInstance(UObject* Object)
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

	#if WITH_EDITOR
	UClass* RedirectClassIfNeeded(UClass* Class)
	{
		if (UCSSkeletonClass* ManagedClass = Cast<UCSSkeletonClass>(Class))
		{
			return ManagedClass->GetGeneratedClass();
		}

		return Class;
	}
	#endif

	bool IsChildOf(UClass* ChildClass, UClass* ParentClass)
	{
		 if (!IsValid(ChildClass) || !IsValid(ParentClass))
		 {
			 return false;
		 }
		
	#if WITH_EDITOR
		ChildClass = RedirectClassIfNeeded(ChildClass);
		ParentClass = RedirectClassIfNeeded(ParentClass);
	#endif
		
		 return ChildClass->IsChildOf(ParentClass);
	}
	
	BIND_UNREALSHARP_FUNCTION(GetNativeFunctionFromClassAndName)
	BIND_UNREALSHARP_FUNCTION(GetNativeFunctionFromInstanceAndName)
	BIND_UNREALSHARP_FUNCTION(GetFirstNativeImplementationFromInstanceAndName)
	BIND_UNREALSHARP_FUNCTION(GetDefaultFromName)
	BIND_UNREALSHARP_FUNCTION(GetDefaultFromInstance)
	BIND_UNREALSHARP_FUNCTION(IsChildOf)
}
