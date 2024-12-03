#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "CSMulticastDelegatePropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSMulticastDelegatePropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()

protected:

	// Begin UCSPropertyGenerator interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::MulticastInlineDelegate; }
	virtual FFieldClass* GetPropertyClass() override { return FMulticastInlineDelegateProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	// End UCSPropertyGenerator interface
	
};
