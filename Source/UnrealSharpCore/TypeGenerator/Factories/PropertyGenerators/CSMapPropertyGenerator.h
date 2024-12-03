#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "CSMapPropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSMapPropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()
protected:
	// Begin UCSPropertyGenerator interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Map; }
	virtual FFieldClass* GetPropertyClass() override { return FMapProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	// End UCSPropertyGenerator interface
};
