#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "CSDelegatePropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSDelegatePropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()

protected:
	// Begin UCSPropertyGenerator interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Delegate; }
	virtual FFieldClass* GetPropertyClass() override { return FDelegateProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	// End UCSPropertyGenerator interface
};
