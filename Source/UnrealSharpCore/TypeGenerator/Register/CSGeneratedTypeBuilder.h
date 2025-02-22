#pragma once

#include "UObject/Class.h"
#include "UObject/Field.h"
#include "UnrealSharpCore/CSManager.h"

template<typename TMetaData, class TField>
class TCSGeneratedTypeBuilder
{
public:
	
	virtual ~TCSGeneratedTypeBuilder() = default;
	
	TCSGeneratedTypeBuilder(TSharedPtr<TMetaData> InTypeMetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TypeMetaData(InTypeMetaData), Field(nullptr), OwningAssembly(InOwningAssembly)
	{
	}

	TCSGeneratedTypeBuilder(TSharedPtr<TMetaData> InTypeMetaData, TField* InField) : TypeMetaData(InTypeMetaData), Field(InField)
	{
	}

	TCSGeneratedTypeBuilder() : Field(nullptr)
	{
		
	}

	TField* CreateType()
	{
		UPackage* Package = OwningAssembly->GetPackage(TypeMetaData->FieldName.GetNamespace());
		FName FieldName = GetFieldName();

#if WITH_EDITOR
		Field = FindObject<TField>(Package, *FieldName.ToString());
		if (!Field)
#endif
		{
			Field = NewObject<TField>(Package, TField::StaticClass(), FieldName, RF_Public | RF_Standalone);
		}
		
		return Field;
	}

	// Start TCSGeneratedTypeBuilder interface
	virtual void RebuildType() = 0;
	virtual void UpdateType() = 0;
	virtual FName GetFieldName() const { return TypeMetaData->FieldName.GetName(); }
	// End of interface

	void RegisterFieldToLoader(ENotifyRegistrationType RegistrationType)
	{
		NotifyRegistrationEvent(*Field->GetOutermost()->GetName(),
		*Field->GetName(),
		RegistrationType,
		ENotifyRegistrationPhase::NRP_Finished,
		nullptr,
		false,
		Field);
	}

protected:
	
	TSharedPtr<const TMetaData> TypeMetaData;
	TField* Field;
	TSharedPtr<FCSAssembly> OwningAssembly;
	
};

