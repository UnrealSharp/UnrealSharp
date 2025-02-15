#pragma once

#include "CSUnrealSharpSettings.h"
#include "CSMetaDataUtils.h"
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
		UPackage* Package = OwningAssembly->GetPackage(TypeMetaData->Namespace);
		FName FieldName = GetFieldName();

#if WITH_EDITOR
		Field = FindObject<TField>(Package, *FieldName.ToString());
		if (!Field)
#endif
		{
			Field = NewObject<TField>(Package, TField::StaticClass(), FieldName, RF_Public | RF_Standalone);
		}

#if WITH_EDITOR
		FCSMetaDataUtils::ApplyMetaData(TypeMetaData->MetaData, Field);
		ApplyDisplayName();
#endif
		
		return Field;
	}

	// Start TCSGeneratedTypeBuilder interface
	virtual void RebuildType() = 0;
	virtual void UpdateType() = 0;
	virtual FName GetFieldName() const { return TypeMetaData->Name; }
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

private:

	void ApplyDisplayName()
	{
#if WITH_EDITOR
		static FString DisplayNameKey = TEXT("DisplayName");
		if (!Field->HasMetaData(*DisplayNameKey))
		{
			Field->SetMetaData(*DisplayNameKey, *TypeMetaData->Name.ToString());
		}
		
		if (GetDefault<UCSUnrealSharpSettings>()->bSuffixGeneratedTypes)
		{
			FString DisplayName = Field->GetMetaData(*DisplayNameKey);
			DisplayName += TEXT(" (C#)");
			Field->SetMetaData(*DisplayNameKey, *DisplayName);
		}
#endif
	}
};

