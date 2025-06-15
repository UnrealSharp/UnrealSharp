#pragma once

#include "CSMetaDataUtils.h"
#include "CSAssembly.h"
#include "UObject/Class.h"
#include "UObject/Field.h"

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
#if WITH_EDITOR
	virtual void UpdateType() = 0;
#endif
	virtual FName GetFieldName() const
	{
		return FCSMetaDataUtils::GetAdjustedFieldName(TypeMetaData->FieldName);
	}
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
	TSharedPtr<struct FCSAssembly> OwningAssembly;
};

