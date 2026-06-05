#pragma once

#include "Engine/DeveloperSettings.h"
#include "Types/CSClass.h"
#include "Types/CSInterface.h"
#include "Types/CSSkeletonClass.h"

class UNREALSHARPCORE_API FCSClassUtilities
{
public:
	static bool IsManagedClass(const UClass* Class)
	{
#if WITH_EDITOR
		UClass* ClassObj = Class->GetClass();
		return ClassObj == UCSClass::StaticClass() || ClassObj == UCSSkeletonClass::StaticClass();
#else
		return Class->GetClass() == UCSClass::StaticClass();
#endif
	}

	static bool IsSkeletonType(const UClass* Class) { return Class->GetClass() == UCSSkeletonClass::StaticClass(); }
	static bool IsNativeClass(UClass* Class){ return Class->GetClass() == UClass::StaticClass(); }

	static bool IsDeveloperSettingsClass(const UBlueprint* Blueprint, const UClass* NewClass)
	{
		return Blueprint->GeneratedClass == NewClass && NewClass->IsChildOf<UDeveloperSettings>();
	}

	static UCSClass* GetFirstManagedClass(UClass* Class)
	{
		while (Class && !IsManagedClass(Class))
		{
			Class = Class->GetSuperClass();

			if (IsNativeClass(Class))
			{
				// We've already reached a native class, so we can stop searching.
				return nullptr;
			}
		}
	
		return (UCSClass*) Class;
	}
	
	static ICSManagedTypeInterface* GetManagedType(UClass* Class)
	{
		for (UClass* It = Class; It; It = It->GetSuperClass())
		{
			if (ICSManagedTypeInterface* Managed = Cast<ICSManagedTypeInterface>(It))
			{
				return Managed;
			}
			
			if (IsNativeClass(It))
			{
				return nullptr;
			}
		}
		
		return nullptr;
	}

	
	static UClass* GetFirstNativeClass(UClass* Class)
	{
		while (!IsNativeClass(Class))
		{
			Class = Class->GetSuperClass();
		}
	
		return Class;
	}

	static UClass* GetFirstNonBlueprintClass(UClass* InClass)
	{
		UClass* CurrentClass = InClass;
		
		while (CurrentClass)
		{
			UPackage* ClassPackage = CurrentClass->GetPackage();
			
			if (!IsBlueprintField(ClassPackage))
			{
				break;
			}

			CurrentClass = CurrentClass->GetSuperClass();
		}
		
		return CurrentClass;
	}
	
	static bool IsBlueprintField(UPackage* FieldPackage)
	{
		return FieldPackage && !FieldPackage->HasAnyPackageFlags(PKG_CompiledIn);
	}
	
	static bool IsBlueprintObject(const UObject* Object)
	{
		return IsBlueprintField(Object->GetPackage());
	}

	static bool HasImplementedFunction(const UClass* Class, const FName& FunctionName)
	{
		UFunction* Function = Class->FindFunctionByName(FunctionName);
		return IsValid(Function) && Function->GetOuter()->IsA(UBlueprintGeneratedClass::StaticClass());
	}
};
