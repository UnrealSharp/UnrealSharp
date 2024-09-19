#pragma once

#include "UObject/UnrealType.h"
#include "TypeGenerator/Register/MetaData/CSPropertyType.h"

class FCSDefaultComponentProperty;
struct FCSPropertyMetaData;
class FCSPropertyFactory;
class FProperty;

using FMakeNewPropertyDelegate = FProperty* (*)(UField*, const FCSPropertyMetaData&);

class FCSPropertyFactory
{
	
public:

	static void InitializePropertyFactory();
	
	static FProperty* CreateAndAssignProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	static FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	
	static void GeneratePropertiesForType(UField* Outer, const TArray<FCSPropertyMetaData>& PropertiesMetaData);
	
	static void AddProperty(ECSPropertyType PropertyType, FMakeNewPropertyDelegate Function);

	template<typename PrimitiveProperty = FProperty>
	static void AddSimpleProperty(ECSPropertyType PropertyType);
 
	template <class FieldClass = FProperty>
	static FieldClass* CreateSimpleProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);

	static FProperty* CreateObjectProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	static FProperty* CreateWeakObjectProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	static FProperty* CreateSoftObjectProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	static FProperty* CreateObjectPtrProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	static FProperty* CreateSoftClassProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	
	static FProperty* CreateClassProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	static FProperty* CreateStructProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	static FProperty* CreateArrayProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	static FProperty* CreateEnumProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	static FProperty* CreateDelegateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	static FProperty* CreateMulticastDelegateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);

	static FProperty* CreateMapProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	
	static bool IsOutParameter(const FProperty* InParam);

	static bool CanBeHashed(const FProperty* InParam);

private:

	template<typename ObjectProperty>
	static ObjectProperty* CreateObjectProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	
};
