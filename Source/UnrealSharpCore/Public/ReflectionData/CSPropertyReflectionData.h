#pragma once

#include "CSTypeReferenceReflectionData.h"
#include "CSUnrealType.h"

struct FCSFunctionReflectionData;

struct UNREALSHARPCORE_API FCSPropertyReflectionData : FCSTypeReferenceReflectionData
{
	virtual ~FCSPropertyReflectionData() override = default;

	// FCSReflectionDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override;
	// End of FCSReflectionDataBase interface

	FName GetName() const { return FieldName.GetFName(); }
	bool HasGetterOrSetter() const { return GetterMethod.IsValid() || SetterMethod.IsValid(); }

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
	FName ReplicatedUsing;
	ELifetimeCondition LifetimeCondition;
	
	TSharedPtr<FCSFunctionReflectionData> GetterMethod;
	TSharedPtr<FCSFunctionReflectionData> SetterMethod;
};
