#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"

namespace FUnrealSharpUtils
{
	UNREALSHARPUTILITIES_API FName GetNamespace(const UObject* Object);
	UNREALSHARPUTILITIES_API FName GetNamespace(FName PackageName);
	UNREALSHARPUTILITIES_API FName GetNativeFullName(const UField* Object);
	
	UNREALSHARPUTILITIES_API FName GetModuleName(const UObject* Object);

	UNREALSHARPUTILITIES_API bool IsStandalonePIE();

	UNREALSHARPUTILITIES_API void PurgeStruct(UStruct* Struct);

	template<typename T>
	static void GetAllCDOsOfClass(TArray<T*>& OutObjects)
	{
		for (TObjectIterator<UClass> It; It; ++It)
		{
			UClass* ClassObject = *It;
		
			if (!ClassObject->IsChildOf(T::StaticClass()) || ClassObject->HasAnyClassFlags(CLASS_Abstract))
			{
				continue;
			}

			T* CDO = ClassObject->GetDefaultObject<T>();
			OutObjects.Add(CDO);
		}
	}
};
