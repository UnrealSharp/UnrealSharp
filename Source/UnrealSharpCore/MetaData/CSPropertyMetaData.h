#pragma once

#include "CSTypeReferenceMetaData.h"
#include "CSUnrealType.h"

struct UNREALSHARPCORE_API FCSPropertyMetaData : FCSTypeReferenceMetaData
{
	virtual ~FCSPropertyMetaData() override = default;
	
	FCSPropertyMetaData() 
		: Type(nullptr)
		, Flags(CPF_None)
		, ArrayDim(1)
		, RepNotifyFunctionName(NAME_None)
		, LifetimeCondition(COND_None)
	{
		
	}
	
	TSharedPtr<FCSUnrealType> Type;
	EPropertyFlags Flags;
	int32 ArrayDim = 0;
	FName RepNotifyFunctionName;
	ELifetimeCondition LifetimeCondition;
	FString BlueprintSetter;
	FString BlueprintGetter;

	FName GetName() const { return FieldName.GetFName(); }

	template<typename T>
	TSharedPtr<T> GetTypeMetaData() const
	{
		return StaticCastSharedPtr<T>(Type);
	}
};
