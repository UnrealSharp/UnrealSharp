// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "ManagedReferencesCollection.h"
#include "CSManagedTypeDefinition.h"
#include "UObject/Interface.h"
#include "CSManagedTypeInterface.generated.h"

UINTERFACE()
class UCSManagedTypeInterface : public UInterface
{
	GENERATED_BODY()
};

class ICSManagedTypeInterface
{
	GENERATED_BODY()
public:
	void SetManagedTypeDefinition(const TSharedPtr<FCSManagedTypeDefinition>& InManagedTypeDefinition)
	{
		ManagedTypeDefinition = InManagedTypeDefinition;
	}

	bool HasManagedTypeDefinition() const
	{
		return ManagedTypeDefinition.IsValid();
	}
	
	TSharedPtr<FCSManagedTypeDefinition> GetManagedTypeDefinition() const
	{
		ensureMsgf(ManagedTypeDefinition.IsValid(), TEXT("ManagedTypeDefinition is not set. Call SetManagedTypeDefinition() first."));
		return ManagedTypeDefinition;
	}

	template<typename TReflectionData = FCSTypeReferenceReflectionData>
	TSharedPtr<TReflectionData> GetReflectionData() const
	{
		return ManagedTypeDefinition->GetReflectionData<TReflectionData>();
	}

	UCSManagedAssembly* GetOwningAssembly() const
	{
		ensureMsgf(ManagedTypeDefinition.IsValid(), TEXT("ManagedTypeDefinition is not set. Call SetManagedTypeDefinition() first."));
		return ManagedTypeDefinition->GetOwningAssembly();
	}

#if WITH_EDITOR
	FCSManagedReferencesCollection& GetManagedReferencesCollection() { return ManagedReferences; }
#endif
	
private:
	TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition;
#if WITH_EDITORONLY_DATA
	FCSManagedReferencesCollection ManagedReferences;
#endif
};
