#include "Export/UClassExporter.h"

#include "CSManagedAssembly.h"
#include "CSManager.h"
#include "Engine/Blueprint.h"
#include "Engine/BlueprintGeneratedClass.h"
#include "Types/CSSkeletonClass.h"
#include "Utilities/CSClassUtilities.h"
#include "UnrealSharpCore.h"

UFunction* UUClassExporter::GetNativeFunctionFromClassAndName(const UClass* Class, const char* FunctionName)
{
	if (!IsValid(Class))
	{
		UE_LOGFMT(LogUnrealSharp, Warning, "Failed to get NativeFunction for class. Class is not valid. FunctionName: {0}", FunctionName);
		return nullptr;
	}

	UFunction* Function = Class->FindFunctionByName(FunctionName);

#if WITH_EDITOR
	// Editor-only: In the editor, the type pointer we cache on the managed side often points to the SkeletonGeneratedClass.
	// For RPCs, the skeleton can be missing runtime replication metadata (e.g. RPCId), which may crash on the very first call
	// of a newly created RPC. Prefer resolving Net functions against the actual GeneratedClass when possible.
	if (IsValid(Function) && Function->HasAnyFunctionFlags(FUNC_Net))
	{
		if (const UCSSkeletonClass* SkeletonClass = Cast<UCSSkeletonClass>(Class))
		{
			if (UCSClass* GeneratedClass = SkeletonClass->GetGeneratedClass())
			{
				if (UFunction* GeneratedFunction = GeneratedClass->FindFunctionByName(FunctionName))
				{
					return GeneratedFunction;
				}
			}
		}

		// Fallback: regular Blueprint classes can also have skeleton/generated mismatches (not just UCSSkeletonClass).
		if (const UBlueprintGeneratedClass* BlueprintGeneratedClass = Cast<UBlueprintGeneratedClass>(Class))
		{
			if (const UBlueprint* Blueprint = Cast<UBlueprint>(BlueprintGeneratedClass->ClassGeneratedBy))
			{
				if (UClass* GeneratedClass = Blueprint->GeneratedClass; IsValid(GeneratedClass) && GeneratedClass != Class)
				{
					if (UFunction* GeneratedFunction = GeneratedClass->FindFunctionByName(FunctionName))
					{
						return GeneratedFunction;
					}
				}
			}
		}
	}
#endif

	// Non-Net functions: Prefer looking up on the provided Class. During editor compilation, many UFunctions only exist
	// on the SkeletonGeneratedClass.
	if (IsValid(Function))
	{
		return Function;
	}

#if WITH_EDITOR
	// If not found on the provided class, try the GeneratedClass. At runtime/PIE, only the GeneratedClass may have
	// the complete function table (e.g. newly created RPCs).
	if (const UCSSkeletonClass* SkeletonClass = Cast<UCSSkeletonClass>(Class))
	{
		if (UCSClass* GeneratedClass = SkeletonClass->GetGeneratedClass())
		{
			Function = GeneratedClass->FindFunctionByName(FunctionName);
			if (IsValid(Function))
			{
				return Function;
			}
		}
	}

	if (const UBlueprintGeneratedClass* BlueprintGeneratedClass = Cast<UBlueprintGeneratedClass>(Class))
	{
		if (const UBlueprint* Blueprint = Cast<UBlueprint>(BlueprintGeneratedClass->ClassGeneratedBy))
		{
			if (UClass* GeneratedClass = Blueprint->GeneratedClass; IsValid(GeneratedClass) && GeneratedClass != Class)
			{
				Function = GeneratedClass->FindFunctionByName(FunctionName);
				if (IsValid(Function))
				{
					return Function;
				}
			}
		}
	}
#endif

	if (!IsValid(Function))
	{
		UE_LOGFMT(LogUnrealSharp, Warning, "Failed to get NativeFunction. Class: {0}, FunctionName: {1}", *Class->GetName(), FunctionName);
		return nullptr;
	}

	return Function;
}

UFunction* UUClassExporter::GetNativeFunctionFromInstanceAndName(const UObject* NativeObject, const char* FunctionName)
{
	if (!IsValid(NativeObject))
	{
		UE_LOGFMT(LogUnrealSharp, Warning, "Failed to get NativeFunction. NativeObject is not valid. ObjectName: {0}", *NativeObject->GetName());
		return nullptr;
	}
	
	return NativeObject->FindFunctionChecked(FunctionName);
}

UFunction* UUClassExporter::GetFirstNativeImplementationFromInstanceAndName(const UObject* NativeObject, const char* FunctionName)
{
	if (!IsValid(NativeObject))
	{
		UE_LOGFMT(LogUnrealSharp, Warning, "Failed to get NativeFunction. NativeObject is not valid. ObjectName: {0}", *NativeObject->GetName());
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
		UE_LOGFMT(LogUnrealSharp, Warning, "Failed to get default object for class {0} in assembly {1}.", *FieldName.GetName(), *AssemblyName);
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

bool UUClassExporter::IsChildOf(UClass* ChildClass, UClass* ParentClass)
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
