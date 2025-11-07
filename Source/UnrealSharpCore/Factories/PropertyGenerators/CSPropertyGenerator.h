#pragma once

#include "CoreMinimal.h"
#include "MetaData/CSPropertyMetaData.h"
#include "UObject/Object.h"
#include "CSPropertyGenerator.generated.h"

UCLASS(Abstract)
class UNREALSHARPCORE_API UCSPropertyGenerator : public UObject
{
	GENERATED_BODY()
public:
	
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) { PURE_VIRTUAL() return nullptr; }
	virtual bool SupportsPropertyType(ECSPropertyType InPropertyType) const;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType);

protected:

	virtual ECSPropertyType GetPropertyType() const;
	virtual FFieldClass* GetPropertyClass();
	
	template <typename T>
	T* NewProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData, const FFieldClass* FieldClass = nullptr)
	{
		FProperty* NewProp = NewProperty(Outer, PropertyMetaData, FieldClass);
		return static_cast<T*>(NewProp);
	}
	
	FProperty* NewProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData, const FFieldClass* FieldClass = nullptr);

	static UClass* TryFindingOwningClass(UField* Outer);
	static bool CanBeHashed(const FProperty* InParam);
};
