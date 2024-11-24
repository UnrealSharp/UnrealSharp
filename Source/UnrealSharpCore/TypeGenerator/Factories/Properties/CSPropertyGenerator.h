#pragma once

#include "CoreMinimal.h"
#include "TypeGenerator/Register/MetaData/CSPropertyMetaData.h"
#include "TypeGenerator/Register/MetaData/CSPropertyType.h"
#include "UObject/Object.h"
#include "CSPropertyGenerator.generated.h"

UCLASS(Abstract)
class UNREALSHARPCORE_API UCSPropertyGenerator : public UObject
{
	GENERATED_BODY()

protected:

	virtual ECSPropertyType GetPropertyType() const;
	virtual FFieldClass* GetPropertyClass();
#if WITH_EDITOR
	virtual void CreatePinInfoEditor(const FCSPropertyMetaData& PropertyMetaData, FEdGraphPinType& PinType);
	virtual FName GetPinCategory(const FCSPropertyMetaData& PropertyMetaData) const;
#endif

	static bool CanBeHashed(const FProperty* InParam);

	FProperty* NewProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData, FFieldClass* FieldClass = nullptr);
	
public:

#if WITH_EDITOR
	virtual UObject* GetPinSubCategoryObject(UBlueprint* Blueprint, const FCSPropertyMetaData& PropertyMetaData) const;
#endif
	
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	void CreatePropertyEditor(UBlueprint* Outer, const FCSPropertyMetaData& PropertyMetaData);

	virtual bool SupportsPropertyType(ECSPropertyType InPropertyType) const
	{
		ECSPropertyType PropertyType = GetPropertyType();
		check(PropertyType != ECSPropertyType::Unknown);
		return PropertyType == InPropertyType;
	}
};
