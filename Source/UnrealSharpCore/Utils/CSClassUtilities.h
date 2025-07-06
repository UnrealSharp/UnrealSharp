#pragma once
#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/CSSkeletonClass.h"

class UNREALSHARPCORE_API FCSClassUtilities
{
public:
	static bool IsManagedType(const UClass* Class) { return Class->GetClass() == UCSClass::StaticClass(); }
	static bool IsSkeletonType(const UClass* Class) { return Class->GetClass() == UCSSkeletonClass::StaticClass(); }
	
	static bool IsNativeClass(UClass* Class)
	{
		return Class->GetClass() == UClass::StaticClass();
	}

	static UCSClass* GetFirstManagedClass(UClass* Class)
	{
		if (IsNativeClass(Class))
		{
			return nullptr;
		}
	
		while (Class && !IsManagedType(Class))
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
		while (Class->GetClass() == UBlueprintGeneratedClass::StaticClass())
		{
			Class = Class->GetSuperClass();
		}
	
		return Class;
	}
};
