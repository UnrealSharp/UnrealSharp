#pragma once

#include "UObject/UnrealType.h"
#include "CSharpForUE/TypeGenerator/CSFunction.h"
#include "CSharpForUE/TypeGenerator/Register/CSMetaData.h"

class FCSDefaultComponentProperty;
struct FPropertyMetaData;
class FCSPropertyFactory;
class FProperty;

using FMakeNewPropertyDelegate = FProperty* (*)(UField*, const FPropertyMetaData&, const EPropertyFlags PropertyFlags);

class FCSPropertyFactory
{
	
public:

	static void InitializePropertyFactory();
	
	static FProperty* CreateAndAssignProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags = CPF_None);
	static FProperty* CreateProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags = CPF_None);
	
	static void GeneratePropertiesForType(UField* Outer, const TArray<FPropertyMetaData>& PropertiesMetaData, const EPropertyFlags PropertyFlags = CPF_None);
	
	static void AddProperty(ECSPropertyType PropertyType, FMakeNewPropertyDelegate Function);

	template<typename PrimitiveProperty = FProperty>
	static void AddSimpleProperty(ECSPropertyType PropertyType);
 
	template <class FieldClass = FProperty>
	static FieldClass* CreateSimpleProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags);

	static FProperty* CreateObjectProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags);
	static FProperty* CreateWeakObjectProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags);
	static FProperty* CreateSoftObjectProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags);
	static FProperty* CreateObjectPtrProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags);
	static FProperty* CreateSoftClassProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags);
	
	static FProperty* CreateClassProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags);
	static FProperty* CreateStructProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags);
	static FProperty* CreateArrayProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags);
	static FProperty* CreateEnumProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags);
	static FProperty* CreateDelegateProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags);
	static FProperty* CreateMulticastDelegateProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags);
	
	static bool IsOutParameter(const FProperty* InParam);

private:
	
	static void InitializeVariable(UFunction* Getter, UObject* Outer, FProperty* Property);

	template<typename ObjectProperty>
	static ObjectProperty* CreateObjectProperty(UField* Outer, const FPropertyMetaData& PropertyMetaData, const EPropertyFlags PropertyFlags);
	
};
