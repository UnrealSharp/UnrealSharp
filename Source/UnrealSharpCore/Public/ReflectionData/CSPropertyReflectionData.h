#pragma once

#include "CSTypeReferenceReflectionData.h"
#include "CSUnrealType.h"

struct UNREALSHARPCORE_API FCSPropertyReflectionData : FCSTypeReferenceReflectionData
{
	virtual ~FCSPropertyReflectionData() override = default;

	// FCSReflectionDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override;
	// End of FCSReflectionDataBase interface

	FName GetName() const { return FieldName.GetFName(); }

	template<typename T>
	TSharedPtr<T> GetInnerTypeData() const
	{
		return StaticCastSharedPtr<T>(InnerType);
	}

	template<typename T>
	TSharedPtr<T> SafeCast(ECSPropertyType PropertyType) const
	{
		if (!InnerType.IsValid() || InnerType->PropertyType != PropertyType)
		{
			return nullptr;
		}

		return StaticCastSharedPtr<T>(InnerType);
	}

	TSharedPtr<FCSUnrealType> InnerType;
	EPropertyFlags PropertyFlags;
	int32 ArrayDim = 0;
	FName RepNotifyFunctionName;
	ELifetimeCondition LifetimeCondition;
	FString BlueprintSetter;
	FString BlueprintGetter;
};
