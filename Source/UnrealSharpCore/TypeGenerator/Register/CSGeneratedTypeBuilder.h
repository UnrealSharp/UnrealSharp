#pragma once

#include "CSGeneratedTypeBuilder.generated.h"

struct FCSTypeReferenceMetaData;
struct FCSManagedTypeInfo;
class UCSAssembly;

UCLASS(Abstract)
class UCSGeneratedTypeBuilder : public UObject
{
	GENERATED_BODY()
public:

	UField* CreateType(const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const;

	// Start TCSGeneratedTypeBuilder interface
	virtual void RebuildType(UField* TypeToBuild, const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const { }              
	virtual FString GetFieldName(const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const;
	virtual UClass* GetFieldType() const { return nullptr; }
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
};

