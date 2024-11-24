// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"

DECLARE_LOG_CATEGORY_EXTERN(LogUnrealSharp, Log, All);

class FUnrealSharpCoreModule : public IModuleInterface
{
public:

	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;

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
