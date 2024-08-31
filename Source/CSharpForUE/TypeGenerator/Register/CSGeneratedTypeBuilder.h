#pragma once

#include "CSMetaDataUtils.h"
#include "AssetRegistry/AssetRegistryModule.h"
#include "UObject/Class.h"
#include "UObject/Field.h"
#include "CSharpForUE/CSManager.h"

template<typename TMetaData, class TField>
class TCSGeneratedTypeBuilder
{
public:
	
	virtual ~TCSGeneratedTypeBuilder() = default;
	
	TCSGeneratedTypeBuilder(TSharedPtr<TMetaData> InTypeMetaData) : TypeMetaData(InTypeMetaData), Field(nullptr)
	{
	
	}

	TField* CreateType()
	{
		UPackage* Package = FCSManager::GetUnrealSharpPackage();
		FString FieldName = GetFieldName();
		
		TField* ExistingField = FindObject<TField>(Package, *FieldName);
		
		if (ExistingField)
		{
			if (!ReplaceTypeOnReload())
			{
				Field = ExistingField;
				return Field;
			}
			
			const FString OldPath = ExistingField->GetPathName();
			const FString OldTypeName = FString::Printf(TEXT("%s_OLD_%d"), *ExistingField->GetName(), ExistingField->GetUniqueID());
			ExistingField->Rename(*OldTypeName, nullptr, REN_DontCreateRedirectors);

			IAssetRegistry& AssetRegistry = FModuleManager::LoadModuleChecked<FAssetRegistryModule>(TEXT("AssetRegistry")).Get();
			AssetRegistry.AssetRenamed(ExistingField, OldPath);
		}
		
		Field = NewObject<TField>(Package, TField::StaticClass(), *FieldName, RF_Public | RF_MarkAsRootSet | RF_Transactional);
		
		ApplyBlueprintAccess(Field);
		FCSMetaDataUtils::ApplyMetaData(TypeMetaData->MetaData, Field);

#if WITH_EDITOR
		Field->SetMetaData(TEXT("DisplayName"), *TypeMetaData->Name.ToString());
#endif

		if (ExistingField)
		{
			NewField(ExistingField, Field);
		}
		
		return Field;
	}

	// Start TCSGeneratedTypeBuilder interface
	virtual void StartBuildingType() = 0;
	virtual void NewField(TField* OldField, TField* NewField) {};
	virtual FString GetFieldName() const { return *TypeMetaData->Name.ToString(); }
	virtual bool ReplaceTypeOnReload() const { return true; }
	// End of interface

	void RegisterFieldToLoader(ENotifyRegistrationType RegistrationType)
	{
		NotifyRegistrationEvent(*FCSManager::GetUnrealSharpPackage()->GetName(),
		*Field->GetName(),
		RegistrationType,
		ENotifyRegistrationPhase::NRP_Finished,
		nullptr,
		false,
		Field);
	}

protected:
	
	TSharedPtr<TMetaData> TypeMetaData;
	TField* Field;

private:
	
	static void ApplyBlueprintAccess(UField* Field)
	{
#if WITH_EDITOR
		Field->SetMetaData(TEXT("BlueprintType"), TEXT("true"));
		Field->SetMetaData(TEXT("IsBlueprintBase"), TEXT("true"));
#endif
	}
	
};

