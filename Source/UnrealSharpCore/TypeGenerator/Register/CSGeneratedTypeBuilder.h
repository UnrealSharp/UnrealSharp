#pragma once

#include "CSMetaDataUtils.h"
#include "AssetRegistry/AssetRegistryModule.h"
#include "UObject/Class.h"
#include "UObject/Field.h"
#include "UnrealSharpCore/CSManager.h"

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
		UPackage* Package = UCSManager::Get().GetUnrealSharpPackage();
		FName FieldName = GetFieldName();
		TField* ExistingField = FindObject<TField>(Package, *FieldName.ToString());
		
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

#if WITH_EDITOR
			IAssetRegistry& AssetRegistry = FModuleManager::LoadModuleChecked<FAssetRegistryModule>(TEXT("AssetRegistry")).Get();
			AssetRegistry.AssetRenamed(ExistingField, OldPath);
#endif
		}
		
		Field = CreateField(Package, FieldName);
		
		ApplyBlueprintAccess(Field);
		FCSMetaDataUtils::ApplyMetaData(TypeMetaData->MetaData, Field);

#if WITH_EDITOR
		Field->SetMetaData(TEXT("DisplayName"), *TypeMetaData->Name.ToString());
#endif

		if (ExistingField)
		{
			OnFieldReplaced(ExistingField, Field);
		}
		
		return Field;
	}

	// Start TCSGeneratedTypeBuilder interface
	virtual void StartBuildingType() = 0;
	virtual void OnFieldReplaced(TField* OldField, TField* NewField) {};
	virtual FName GetFieldName() const { return TypeMetaData->Name; }
	virtual bool ReplaceTypeOnReload() const { return true; }
	virtual TField* CreateField(UPackage* Package, const FName FieldName)
	{
		return NewObject<TField>(Package, TField::StaticClass(), FieldName, RF_Public | RF_MarkAsRootSet | RF_Transactional);
	}
	// End of interface

	void RegisterFieldToLoader(ENotifyRegistrationType RegistrationType)
	{
		UPackage* Package = UCSManager::Get().GetUnrealSharpPackage();
		
		NotifyRegistrationEvent(*Package->GetName(),
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

