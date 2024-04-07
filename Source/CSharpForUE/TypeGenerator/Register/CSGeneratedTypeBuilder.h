#pragma once

#include "CSMetaData.h"
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
		TField* ReplacedType = FindObject<TField>(Package, *TypeMetaData->Name);
		
		if (ReplacedType)
		{
			const FString OldTypeName = FString::Printf(TEXT("%s_OLD_%d"), *ReplacedType->GetName(), ReplacedType->GetUniqueID());
			ReplacedType->Rename(*OldTypeName, nullptr, REN_DontCreateRedirectors);
		}
		
		Field = NewObject<TField>(Package, TField::StaticClass(), *TypeMetaData->Name, RF_Public | RF_Standalone | RF_MarkAsRootSet);
		
		ApplyBlueprintAccess(Field);
		FMetaDataHelper::ApplyMetaData(TypeMetaData->MetaData, Field);

		if (ReplacedType)
		{
			NewField(ReplacedType, Field);
		}
		
		return Field;
	}

	// Start TCSGeneratedTypeBuilder interface
	virtual void StartBuildingType() = 0;
	virtual void NewField(TField* OldField, TField* NewField) = 0;
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

