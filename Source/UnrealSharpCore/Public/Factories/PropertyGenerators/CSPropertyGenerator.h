#pragma once

#include "CoreMinimal.h"
#include "ReflectionData/CSPropertyReflectionData.h"
#include "UObject/Object.h"
#include "CSPropertyGenerator.generated.h"

UCLASS(Abstract)
class UNREALSHARPCORE_API UCSPropertyGenerator : public UObject
{
	GENERATED_BODY()
public:
	
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData) { PURE_VIRTUAL() return nullptr; }
	virtual bool SupportsPropertyType(ECSPropertyType InPropertyType) const;
	virtual TSharedPtr<FCSUnrealType> CreatePropertyInnerTypeData(ECSPropertyType PropertyType);

protected:

	virtual ECSPropertyType GetPropertyType() const;
	virtual FFieldClass* GetPropertyClass();

	template <typename T>
	T* NewProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData, const FFieldClass* FieldClass = nullptr)
	{
		FProperty* NewProp = NewProperty(Outer, PropertyReflectionData, FieldClass);
		return static_cast<T*>(NewProp);
	}
	
	FProperty* NewProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData, const FFieldClass* FieldClass = nullptr);

	static UClass* TryFindingOwningClass(UField* Outer);
	static bool CanBeHashed(const FProperty* InParam);
};
