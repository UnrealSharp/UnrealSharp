#pragma once

#include "CoreMinimal.h"
#include "CSDelegateBasePropertyGenerator.h"
#include "CSPropertyGenerator.h"
#include "CSMulticastDelegatePropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSMulticastDelegatePropertyGenerator : public UCSDelegateBasePropertyGenerator
{
	GENERATED_BODY()

protected:

	// Begin UCSPropertyGenerator interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::MulticastInlineDelegate; }
	virtual FFieldClass* GetPropertyClass() override { return FMulticastInlineDelegateProperty::StaticClass(); }
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	// End UCSPropertyGenerator interface
	
};
