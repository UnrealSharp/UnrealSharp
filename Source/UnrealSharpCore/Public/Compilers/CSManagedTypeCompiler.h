#pragma once

#include "CSManagedTypeCompiler.generated.h"

struct FCSTypeReferenceReflectionData;
struct FCSManagedTypeDefinition;
class UCSManagedAssembly;

UCLASS(Abstract, Transient)
class UCSManagedTypeCompiler : public UObject
{
	GENERATED_BODY()
public:

	UField* CreateField(const TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition) const;
	void RecompileManagedTypeDefinition(const TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition) const;

protected:
	// Start UCSManagedTypeCompiler interface
	virtual void Recompile(UField* TypeToRecompile, const TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition) const { }              
	virtual FString GetFieldName(TSharedPtr<const FCSTypeReferenceReflectionData>& ReflectionData) const;
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

