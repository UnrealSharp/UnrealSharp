#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "CSBoolPropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSBoolPropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()
public:
	// Begin UCSPropertyGenerator interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Bool; }
	virtual FFieldClass* GetPropertyClass() override { return FBoolProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData) override;
	virtual TSharedPtr<FCSUnrealType> CreatePropertyInnerTypeData(ECSPropertyType PropertyType) override;
	// End UCSPropertyGenerator interface
};
