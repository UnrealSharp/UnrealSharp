#pragma once

#include "UnrealSharpCore.h"
#include "ReflectionData/CSPropertyReflectionData.h"
#include "ReflectionData/CSPropertyType.h"
#include "UObject/UnrealType.h"

class UCSPropertyGenerator;

class UNREALSHARPCORE_API FCSPropertyFactory
{
public:

	static void Initialize();

	static UCSPropertyGenerator* GetPropertyGenerator(ECSPropertyType PropertyType)
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(FCSPropertyFactory::GetPropertyGenerator);
		
		const uint32 Hash = static_cast<uint32>(PropertyType);
		UCSPropertyGenerator** FoundGenerator = PropertyGeneratorMap.FindByHash(Hash, Hash);
		
		if (!FoundGenerator)
		{
			UE_LOGFMT(LogUnrealSharp, Fatal, "No property generator found for property type: {0}", static_cast<uint8>(PropertyType));
		}

		return *FoundGenerator;
	}
	
	static FProperty* CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData);
	static FProperty* CreateAndAssignProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData)
	{
		FProperty* Property = CreateProperty(Outer, PropertyReflectionData);
		Outer->AddCppProperty(Property);
		return Property;
	}
	
	static void CreateAndAssignProperties(UField* Outer, const TArray<FCSPropertyReflectionData>& PropertyReflectionData, const TFunction<void(FProperty*)>& OnPropertyCreated = nullptr);
	static void TryAddPropertyAsFieldNotify(const FCSPropertyReflectionData& PropertyReflectionData, UBlueprintGeneratedClass* Class);

private:
	static TMap<uint32, UCSPropertyGenerator*> PropertyGeneratorMap;
};
