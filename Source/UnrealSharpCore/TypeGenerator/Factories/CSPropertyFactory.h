#pragma once

#include "MetaData/CSPropertyMetaData.h"
#include "MetaData/CSPropertyType.h"
#include "UObject/UnrealType.h"

class UCSPropertyGenerator;

class UNREALSHARPCORE_API FCSPropertyFactory
{
public:

	static void Initialize();

	static UCSPropertyGenerator* GetPropertyGenerator(ECSPropertyType PropertyType)
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(FCSPropertyFactory::FindPropertyGenerator);
		uint32 Hash = static_cast<uint32>(PropertyType);
		return *PropertyGeneratorMap.FindByHash(Hash, Hash);
	}
	
	static FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	static FProperty* CreateAndAssignProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
	{
		FProperty* Property = CreateProperty(Outer, PropertyMetaData);
		Outer->AddCppProperty(Property);
		return Property;
	}
	
	static void CreateAndAssignProperties(UField* Outer, const TArray<FCSPropertyMetaData>& PropertyMetaData, const TFunction<void(FProperty*)>& OnPropertyCreated = nullptr);

	static void TryAddPropertyAsFieldNotify(const FCSPropertyMetaData& PropertyMetaData, UBlueprintGeneratedClass* Class);

private:
	static TArray<TObjectPtr<UCSPropertyGenerator>> PropertyGenerators;
	static TMap<uint32, UCSPropertyGenerator*> PropertyGeneratorMap;
};
