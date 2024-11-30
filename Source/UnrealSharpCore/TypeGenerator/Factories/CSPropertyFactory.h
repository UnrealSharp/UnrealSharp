#pragma once

#include "UObject/UnrealType.h"
#include "TypeGenerator/Register/MetaData/CSPropertyType.h"

class UCSPropertyGenerator;
class FCSDefaultComponentProperty;
struct FCSPropertyMetaData;
class FCSPropertyFactory;
class FProperty;

using FMakeNewPropertyDelegate = FProperty* (*)(UField*, const FCSPropertyMetaData&);

class FCSPropertyFactory
{
	
public:
	
	static FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	static FProperty* CreateAndAssignProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	static void CreateAndAssignProperties(UField* Outer, const TArray<FCSPropertyMetaData>& PropertyMetaData);
	static UCSPropertyGenerator* FindPropertyGenerator(ECSPropertyType PropertyType);

private:
	static void TryInitializePropertyFactory();
	static TArray<TWeakObjectPtr<UCSPropertyGenerator>> PropertyGenerators;
};
