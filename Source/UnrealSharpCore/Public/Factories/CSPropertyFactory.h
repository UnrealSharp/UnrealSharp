#pragma once

#include "UnrealSharpCore.h"
#include "Logging/StructuredLog.h"
#include "ReflectionData/CSPropertyReflectionData.h"
#include "ReflectionData/CSPropertyType.h"
#include "UObject/UnrealType.h"

class UCSPropertyGenerator;

class FCSPropertyFactory
{
public:
	static UCSPropertyGenerator* GetPropertyGenerator(ECSPropertyType PropertyType)
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(FCSPropertyFactory::GetPropertyGenerator);
		EnsureInitialized();
		
		const uint32 Hash = static_cast<uint32>(PropertyType);
		UCSPropertyGenerator** FoundGenerator = PropertyGeneratorMap.FindByHash(Hash, Hash);
		
		if (!FoundGenerator)
		{
			UE_LOGFMT(LogUnrealSharp, Fatal, "No property generator found for property type: {0}", static_cast<uint8>(PropertyType));
		}

		return *FoundGenerator;
	}
	
	UNREALSHARPCORE_API static FProperty* CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData);
	UNREALSHARPCORE_API static FProperty* CreateAndAssignProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
	{
		FProperty* Property = CreateProperty(Outer, PropertyReflectionData);
		Outer->AddCppProperty(Property);
		return Property;
	}
	
	UNREALSHARPCORE_API static void CreateAndAssignProperties(UField* Outer, const TArray<FCSPropertyReflectionData>& PropertyReflectionData, const TFunction<void(FProperty*)>& OnPropertyCreated = nullptr);
	UNREALSHARPCORE_API static void TryAddPropertyAsFieldNotify(const FCSPropertyReflectionData& PropertyReflectionData, UBlueprintGeneratedClass* Class);

private:
	static void EnsureInitialized();
	static TMap<uint32, UCSPropertyGenerator*> PropertyGeneratorMap;
};
