#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "CSCommonPropertyGenerator.generated.h"

#define REGISTER_METADATA_WITH_NAME(CustomName, MetaDataName) \
MetaDataFactoryMap.Add(CustomName, \
[]() \
{ \
return MakeShared<MetaDataName>(); \
});

#define REGISTER_METADATA(PropertyName, MetaDataName) \
REGISTER_METADATA_WITH_NAME(PropertyName, MetaDataName)

UCLASS(Abstract)
class UNREALSHARPCORE_API UCSCommonPropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()
protected:
	// Begin UCSPropertyGenerator interface
	virtual bool SupportsPropertyType(ECSPropertyType InPropertyType) const override;
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	// End UCSPropertyGenerator interface
	
	TMap<ECSPropertyType, FFieldClass*> TypeToFieldClass;
	TMap<ECSPropertyType, TFunction<TSharedPtr<FCSUnrealType>()>> MetaDataFactoryMap;
};
