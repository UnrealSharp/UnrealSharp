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
public:
	
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData);
	virtual bool SupportsPropertyType(ECSPropertyType InPropertyType) const;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType);

protected:

	virtual ECSPropertyType GetPropertyType() const;
	virtual FFieldClass* GetPropertyClass();

	FProperty* NewProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData, const FFieldClass* FieldClass = nullptr);

	static UClass* TryFindingOwningClass(UField* Outer);
	static bool CanBeHashed(const FProperty* InParam);
};
