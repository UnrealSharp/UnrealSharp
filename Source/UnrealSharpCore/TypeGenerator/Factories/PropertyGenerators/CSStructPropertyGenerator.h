#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "CSStructPropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSStructPropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()

	// Begin UCSPropertyGenerator interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Struct; }
	virtual FFieldClass* GetPropertyClass() override { return FStructProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	// End UCSPropertyGenerator interface
};
