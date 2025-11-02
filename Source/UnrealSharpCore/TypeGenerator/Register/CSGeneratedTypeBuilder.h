#pragma once

#include "CSGeneratedTypeBuilder.generated.h"

struct FCSTypeReferenceMetaData;
struct FCSManagedTypeInfo;
class UCSAssembly;

UCLASS(Abstract, MinimalAPI, Transient)
class UCSGeneratedTypeBuilder : public UObject
{
	GENERATED_BODY()
public:

	UField* CreateField(const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const;
	void TriggerRebuild(UField* FieldToRebuild, const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const;

protected:
	// Start TCSGeneratedTypeBuilder interface
	virtual void RebuildType(UField* TypeToBuild, const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const { }              
	virtual FString GetFieldName(TSharedPtr<const FCSTypeReferenceMetaData>& MetaData) const;
	// End of interface

	static void RegisterFieldToLoader(UField* Field, ENotifyRegistrationType RegistrationType)
	{
		NotifyRegistrationEvent(*Field->GetOutermost()->GetName(),
		*Field->GetName(),
		RegistrationType,
		ENotifyRegistrationPhase::NRP_Finished,
		nullptr,
		false,
		Field);
	}

	UPROPERTY(Transient, DuplicateTransient)
	TObjectPtr<UClass> FieldType;
};

