#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "MetaData/CSTypeReferenceMetaData.h"
#include "CSGeneratedClassBuilder.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSGeneratedClassBuilder : public UCSGeneratedTypeBuilder
{
	GENERATED_BODY()
	DECLARE_BUILDER_TYPE(UCSClass, FCSClassMetaData)
public:
	// TCSGeneratedTypeBuilder interface implementation
	virtual void RebuildType() override;
#if WITH_EDITOR
	virtual void UpdateType() override;
#endif
	virtual FName GetFieldName() const override;
	virtual UClass* GetFieldType() const override;
	// End of implementation
	
	static void ManagedObjectConstructor(const FObjectInitializer& ObjectInitializer);
	static void ImplementInterfaces(UClass* ManagedClass, const TArray<FCSTypeReferenceMetaData>& Interfaces);
	static void SetConfigName(UClass* ManagedClass, const TSharedPtr<const FCSClassMetaData>& TypeMetaData);
	static void SetupDefaultTickSettings(UObject* DefaultObject, const UClass* Class);

	static void TryRegisterDynamicSubsystem(UClass* ManagedClass);
	static void TryUnregisterDynamicSubsystem(UClass* ManagedClass);

private:
#if WITH_EDITOR
	void CreateBlueprint(UClass* SuperClass);
	void CreateClassEditor(UClass* SuperClass);
#endif
	void CreateClass(UClass* SuperClass);
	
	TMap<UClass*, UClass*> RedirectClasses;
};
