#pragma once

#include "CSGeneratedTypeBuilder.generated.h"

struct FCSManagedTypeInfo;
class UCSAssembly;

#define DECLARE_BUILDER_TYPE(TypeClass, MetaDataType) \
virtual void InitializeTypeBuilder(const TSharedPtr<const FCSManagedTypeInfo>& InManagedTypeInfo) override; \
class TypeClass* Field; \
TSharedPtr<const struct MetaDataType> TypeMetaData; \

#define DEFINE_BUILDER_TYPE(ThisClass, TypeClass, MetaDataType) \
void ThisClass::InitializeTypeBuilder(const TSharedPtr<const FCSManagedTypeInfo>& InManagedTypeInfo) \
{ \
	Super::InitializeTypeBuilder(InManagedTypeInfo); \
	Field = GetField<TypeClass>(); \
	TypeMetaData = ManagedTypeInfo->GetTypeMetaData<MetaDataType>(); \
}

UCLASS(Abstract)
class UCSGeneratedTypeBuilder : public UObject
{
	GENERATED_BODY()
public:

	virtual void InitializeTypeBuilder(const TSharedPtr<const FCSManagedTypeInfo>& InManagedTypeInfo)
	{
		ManagedTypeInfo = InManagedTypeInfo;
		FieldToBuild = CreateType();
	}

	UField* CreateType();

	// Start TCSGeneratedTypeBuilder interface
	virtual void RebuildType() { };
#if WITH_EDITOR
	virtual void UpdateType() { };
#endif
	virtual FName GetFieldName() const;
	virtual UClass* GetFieldType() const { return nullptr; }
	// End of interface

	UCSAssembly* GetOwningAssembly() const;

	template<typename TField = UField>
	TField* GetField() const
	{
		static_assert(TIsDerivedFrom<TField, UField>::Value, "T must be a UField-derived type.");
		return static_cast<TField*>(FieldToBuild);
	}
	
	template<typename TMetaData = FCSTypeReferenceMetaData>
	TSharedPtr<TMetaData> GetTypeMetaData() const
	{
		return ManagedTypeInfo->template GetTypeMetaData<TMetaData>();
	}

	void RegisterFieldToLoader(ENotifyRegistrationType RegistrationType)
	{
		NotifyRegistrationEvent(*FieldToBuild->GetOutermost()->GetName(),
		*FieldToBuild->GetName(),
		RegistrationType,
		ENotifyRegistrationPhase::NRP_Finished,
		nullptr,
		false,
		FieldToBuild);
	}

protected:
	UPROPERTY(Transient)
	UField* FieldToBuild;
	
	TSharedPtr<const FCSManagedTypeInfo> ManagedTypeInfo;
};

