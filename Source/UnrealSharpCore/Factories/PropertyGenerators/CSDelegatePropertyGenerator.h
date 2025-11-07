#pragma once

#include "CoreMinimal.h"
#include "CSDelegateBasePropertyGenerator.h"
#include "CSPropertyGenerator.h"
#include "CSDelegatePropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSDelegatePropertyGenerator : public UCSDelegateBasePropertyGenerator
{
	GENERATED_BODY()

protected:
	// Begin UCSPropertyGenerator interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Delegate; }
	virtual FFieldClass* GetPropertyClass() override { return FDelegateProperty::StaticClass(); }
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	// End UCSPropertyGenerator interface
};
