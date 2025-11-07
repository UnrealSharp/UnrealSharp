// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "ManagedReferencesCollection.h"
#include "TypeInfo/CSManagedTypeInfo.h"
#include "UObject/Interface.h"
#include "CSManagedTypeInterface.generated.h"

UINTERFACE()
class UCSManagedTypeInterface : public UInterface
{
	GENERATED_BODY()
};

class UNREALSHARPCORE_API ICSManagedTypeInterface
{
	GENERATED_BODY()
public:
	void SetTypeInfo(const TSharedPtr<FCSManagedTypeInfo>& InTypeMetaData)
	{
		ManagedTypeInfo = InTypeMetaData;
	}

	bool HasTypeInfo() const
	{
		return ManagedTypeInfo.IsValid();
	}
	
	TSharedPtr<FCSManagedTypeInfo> GetManagedTypeInfo() const
	{
		ensureMsgf(ManagedTypeInfo.IsValid(), TEXT("ManagedTypeInfo is not set. Call SetTypeMetaData() first."));
		return ManagedTypeInfo;
	}

	template<typename TMetaData = FCSTypeReferenceMetaData>
	TSharedPtr<TMetaData> GetMetaData() const
	{
		return ManagedTypeInfo->GetMetaData<TMetaData>();
	}

	UCSAssembly* GetOwningAssembly() const
	{
		ensureMsgf(ManagedTypeInfo.IsValid(), TEXT("ManagedTypeInfo is not set. Call SetTypeMetaData() first."));
		return ManagedTypeInfo->GetOwningAssembly();
	}

	FCSManagedReferencesCollection& GetManagedReferencesCollection() { return ManagedReferences; }
	
private:
	TSharedPtr<FCSManagedTypeInfo> ManagedTypeInfo;
#if WITH_EDITORONLY_DATA
	FCSManagedReferencesCollection ManagedReferences;
#endif
};
