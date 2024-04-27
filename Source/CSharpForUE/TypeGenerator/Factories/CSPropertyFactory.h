#pragma once

#include "UObject/UnrealType.h"
#include "CSharpForUE/TypeGenerator/CSFunction.h"
#include "CSharpForUE/TypeGenerator/Register/CSMetaData.h"

class FCSDefaultComponentProperty;
struct FPropertyMetaData;
class FCSPropertyFactory;
class FProperty;

using FMakeNewPropertyDelegate = FProperty* (*)(UField*, const FPropertyMetaData&);

class FCSPropertyFactory
{
	
public:

	static void InitializePropertyFactory();
	
	static FProperty* CreateAndAssignProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData);
	static FProperty* CreateProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData);
	
	static void GeneratePropertiesForType(UField* Outer, const TArray<FPropertyMetaData>& PropertiesMetaData);
	
	static void AddProperty(ECSPropertyType PropertyType, FMakeNewPropertyDelegate Function);

	template<typename PrimitiveProperty = FProperty>
	static void AddSimpleProperty(ECSPropertyType PropertyType);
 
	template <class FieldClass = FProperty>
	static FieldClass* CreateSimpleProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData);

	static FProperty* CreateObjectProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData);
	static FProperty* CreateWeakObjectProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData);
	static FProperty* CreateSoftObjectProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData);
	static FProperty* CreateObjectPtrProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData);
	static FProperty* CreateSoftClassProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData);
	
	static FProperty* CreateClassProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData);
	static FProperty* CreateStructProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData);
	static FProperty* CreateArrayProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData);
	static FProperty* CreateEnumProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData);
	static FProperty* CreateDelegateProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData);
	static FProperty* CreateMulticastDelegateProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData);
	
	static bool IsOutParameter(const FProperty* InParam);

private:

	template<typename ObjectProperty>
	static ObjectProperty* CreateObjectProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData);
	
};
