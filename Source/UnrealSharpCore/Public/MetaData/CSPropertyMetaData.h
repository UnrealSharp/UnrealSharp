#pragma once

#include "CSTypeReferenceMetaData.h"
#include "CSUnrealType.h"

struct UNREALSHARPCORE_API FCSPropertyMetaData : FCSTypeReferenceMetaData
{
	virtual ~FCSPropertyMetaData() override = default;

	// FCSMetaDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override;
	// End of FCSMetaDataBase interface

	FName GetName() const { return FieldName.GetFName(); }

	template<typename T>
	TSharedPtr<T> GetTypeMetaData() const
	{
		return StaticCastSharedPtr<T>(Type);
	}

	template<typename T>
	TSharedPtr<T> SafeCastTypeMetaData(ECSPropertyType PropertyType) const
	{
		if (!Type.IsValid() || Type->PropertyType != PropertyType)
		{
			return nullptr;
		}

		return StaticCastSharedPtr<T>(Type);
	}

	TSharedPtr<FCSUnrealType> Type;
	EPropertyFlags PropertyFlags;
	int32 ArrayDim = 0;
	FName RepNotifyFunctionName;
	ELifetimeCondition LifetimeCondition;
	FString BlueprintSetter;
	FString BlueprintGetter;
};
