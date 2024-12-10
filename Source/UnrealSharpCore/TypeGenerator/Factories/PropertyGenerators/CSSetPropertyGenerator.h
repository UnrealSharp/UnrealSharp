#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "CSSetPropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSSetPropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()
protected:
	// Begin UCSPropertyGenerator interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Set; }
	virtual FFieldClass* GetPropertyClass() override { return FSetProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	// End UCSPropertyGenerator interface
};
