#pragma once

#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/CSInterface.h"
#include "TypeGenerator/CSSkeletonClass.h"

class UNREALSHARPCORE_API FCSClassUtilities
{
public:
	static bool IsManagedClass(const UClass* Class) { return Class->GetClass() == UCSClass::StaticClass(); }
	static bool IsManagedType(const UClass* Class);
	static bool IsSkeletonType(const UClass* Class) { return Class->GetClass() == UCSSkeletonClass::StaticClass(); }
	static bool IsNativeClass(UClass* Class){ return Class->GetClass() == UClass::StaticClass(); }

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

	static UClass* GetFirstNonBlueprintClass(UClass* Class)
	{
		while (Class->GetClass() != UClass::StaticClass() && Class->GetClass() != UCSClass::StaticClass())
		{
			Class = Class->GetSuperClass();
		}
	
		return Class;
	}
};
