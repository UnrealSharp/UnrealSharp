#pragma once

#include "TypeGenerator/Register/MetaData/CSPropertyMetaData.h"
#include "UObject/UnrealType.h"
#include "TypeGenerator/Register/MetaData/CSPropertyType.h"

class UCSPropertyGenerator;

class FCSPropertyFactory
{
public:
	static void Initialize();
	
	static FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	static FProperty* CreateAndAssignProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	static void CreateAndAssignProperties(UField* Outer, const TArray<FCSPropertyMetaData>& PropertyMetaData);
	
	static TSharedPtr<FCSUnrealType> CreateTypeMetaData(const TSharedPtr<FJsonObject>& PropertyMetaData);

private:
	static TArray<TWeakObjectPtr<UCSPropertyGenerator>> PropertyGenerators;
	static UCSPropertyGenerator* FindPropertyGenerator(ECSPropertyType PropertyType);
};
