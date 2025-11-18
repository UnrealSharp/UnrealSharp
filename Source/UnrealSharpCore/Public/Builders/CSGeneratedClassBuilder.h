#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "MetaData/CSTypeReferenceMetaData.h"
#include "CSGeneratedClassBuilder.generated.h"

struct FCSClassMetaData;
class UCSClass;

UCLASS()
class UNREALSHARPCORE_API UCSGeneratedClassBuilder : public UCSGeneratedTypeBuilder
{
	GENERATED_BODY()
public:
	UCSGeneratedClassBuilder();
	
	// TCSGeneratedTypeBuilder interface implementation
	virtual void RebuildType(UField* TypeToBuild, const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const override;
	virtual FString GetFieldName(TSharedPtr<const FCSTypeReferenceMetaData>& MetaData) const override;
	// End of implementation
	
	static void ManagedObjectConstructor(const FObjectInitializer& ObjectInitializer);
	static void ImplementInterfaces(UClass* ManagedClass, const TArray<FCSTypeReferenceMetaData>& Interfaces);
	static void SetConfigName(UClass* ManagedClass, const TSharedPtr<const FCSClassMetaData>& TypeMetaData);
	static void SetupDefaultTickSettings(UObject* DefaultObject, const UClass* Class);

	static void TryRegisterDynamicSubsystem(UClass* ManagedClass);
	static void TryUnregisterDynamicSubsystem(UClass* ManagedClass);

private:
#if WITH_EDITOR
	static void CreateOrUpdateOwningBlueprint(TSharedPtr<FCSClassMetaData> TypeMetaData, UCSClass* Field, UClass* SuperClass);
#endif
	static void CreateClass(TSharedPtr<FCSClassMetaData> TypeMetaData, UCSClass* Field, UClass* SuperClass);

	UClass* TryRedirectSuperClass(TSharedPtr<FCSClassMetaData> TypeMetaData, UClass* SuperClass) const;
	
	TMap<TObjectKey<UClass>, TWeakObjectPtr<UClass>> RedirectClasses;
};
