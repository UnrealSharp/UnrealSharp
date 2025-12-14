#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "CSCommonPropertyGenerator.generated.h"

#define REGISTER_REFLECTION_DATA_WITH_NAME(CustomName, InnerTypeName) \
ReflectionDataFactoryMap.Add(CustomName, \
[]() \
{ \
return MakeShared<InnerTypeName>(); \
});

#define REGISTER_REFLECTION_DATA(PropertyName, InnerTypeName) \
REGISTER_REFLECTION_DATA_WITH_NAME(PropertyName, InnerTypeName)

UCLASS(Abstract)
class UNREALSHARPCORE_API UCSCommonPropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()
protected:
	// Begin UCSPropertyGenerator interface
	virtual bool SupportsPropertyType(ECSPropertyType InPropertyType) const override;
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData) override;
	virtual TSharedPtr<FCSUnrealType> CreatePropertyInnerTypeData(ECSPropertyType PropertyType) override;
	// End UCSPropertyGenerator interface

	FFieldClass* GetFieldClassForType(const FCSPropertyReflectionData& PropertyReflectionData) const
	{
		return TypeToFieldClass.FindChecked(PropertyReflectionData.InnerType->PropertyType);
	}
	
	TMap<ECSPropertyType, FFieldClass*> TypeToFieldClass;
	TMap<ECSPropertyType, TFunction<TSharedPtr<FCSUnrealType>()>> ReflectionDataFactoryMap;
};
