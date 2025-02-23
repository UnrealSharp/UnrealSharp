#pragma once

#include "CSPropertyType.h"

struct FCSUnrealType
{
	virtual ~FCSUnrealType() = default;
	
	ECSPropertyType PropertyType = ECSPropertyType::Unknown;

	// Begin FCSUnrealType
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject);
	virtual bool IsEqual(TSharedPtr<FCSUnrealType> Other) const;
	// End FCSUnrealType

	template<typename T>
	TSharedPtr<T> SafeCast(const TSharedPtr<FCSUnrealType> Other) const
	{
		if (Other.IsValid() && Other->PropertyType == PropertyType)
		{
			return StaticCastSharedPtr<T>(Other);
		}
		
		return nullptr;
	}
};
