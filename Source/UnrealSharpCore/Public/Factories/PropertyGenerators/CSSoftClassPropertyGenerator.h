#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "CSSoftClassPropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSSoftClassPropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()

protected:

	// Begin UCSPropertyGenerator interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::SoftClass; }
	virtual FFieldClass* GetPropertyClass() override { return FSoftClassProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData) override;
	virtual TSharedPtr<FCSUnrealType> CreatePropertyInnerTypeData(ECSPropertyType PropertyType) override;
	// End UCSPropertyGenerator interface
};
