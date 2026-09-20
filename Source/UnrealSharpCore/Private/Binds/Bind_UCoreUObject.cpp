#include "CSBindsRegistry.h"
#include "CSManagedAssembly.h"
#include "CSManager.h"
#include "Logging/StructuredLog.h"

DECLARE_UNREALSHARP_BINDER(Bind_UCoreUObject)
{
	UField* GetNativeField(const char* InAssemblyName, const char* InNamespace, const char* InTypeName, ECSFieldType InFieldType)
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(Bind_UCoreUObject::GetType);
		UCSManagedAssembly* Assembly = UCSManager::Get().FindOrLoadAssembly(InAssemblyName);
		ensure(Assembly);

		FCSFieldName FieldName(InTypeName, FCSNamespace(InNamespace), InAssemblyName, InFieldType);
		TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition = Assembly->FindOrAddManagedTypeDefinition(FieldName);
		UField* Field = ManagedTypeDefinition->GetDefinition();

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

	UDelegateFunction* GetNativeDelegate(const char* PackageName, const char* OuterName, const char* DelegateName)
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(GetNativeDelegate);
		
		UPackage* Package = FindPackage(nullptr, UTF8_TO_TCHAR(PackageName));
		
		if (!IsValid(Package))
		{
			UE_LOGFMT(LogUnrealSharp, Fatal, "GetNativeDelegate: Could not find package %s", PackageName);
		}
		
		UObject* Outer = Package;
		if (OuterName)
		{
			Outer = FindObject<UObject>(Package, UTF8_TO_TCHAR(OuterName));
			if (!IsValid(Outer))
			{
				UE_LOGFMT(LogUnrealSharp, Fatal, "GetNativeDelegate: Could not find outer %s in package %s", OuterName, PackageName);
			}
		}
		
		UDelegateFunction* DelegateFunction = FindObject<UDelegateFunction>(Outer, UTF8_TO_TCHAR(DelegateName));
		if (!IsValid(DelegateFunction))
		{
			UE_LOGFMT(LogUnrealSharp, Fatal, "GetNativeDelegate: Could not find delegate %s in outer %s in package %s", DelegateName, OuterName ? OuterName : "<null>", PackageName);
		}
		
		return DelegateFunction;
	}

	UField* GetGeneratedClassFromSkeleton(UField* InType)
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(GetGeneratedClassFromSkeleton);
		
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
	
	BIND_UNREALSHARP_FUNCTION(GetNativeField)
	BIND_UNREALSHARP_FUNCTION(GetNativeDelegate)
	BIND_UNREALSHARP_FUNCTION(GetGeneratedClassFromSkeleton)
}